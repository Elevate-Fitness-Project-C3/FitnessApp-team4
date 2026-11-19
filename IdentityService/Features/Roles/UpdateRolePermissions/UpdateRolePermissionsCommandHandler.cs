using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler(AuthDbContext db, SecurityMutationChecks checks, EffectivePermissionResolver permissions, AuditActorContext actor, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<UpdateRolePermissionsCommand, Result<RoleDto>>
{
    public Task<Result<RoleDto>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Roles.Permissions.Update", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<RoleDto>> ApplyAsync(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (role is null) return SecurityMutationChecks.Missing;
        if (role.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        // Public registration assigns User automatically. It cannot become a security role.
        if (SecurityMutationChecks.IsDefaultRole(role) || !await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        var known = await db.Set<Permission>().Where(x => x.IsActive).Select(x => x.Code).ToListAsync(cancellationToken);
        if (request.PermissionCodes.Any(x => !known.Contains(x, StringComparer.Ordinal))) return Error.Validation("Permissions.Unknown", "An active catalog permission is required.");
        var own = await permissions.ResolveAsync(actor.UserId!, cancellationToken, fresh: true);
        if (request.PermissionCodes.Any(x => !own.Contains(x))) return Error.Forbidden();
        var existing = await db.Set<IdentityRoleClaim<string>>().Where(x => x.RoleId == role.Id && x.ClaimType == "permission").ToListAsync(cancellationToken);
        var old = existing.Select(x => x.ClaimValue!).ToHashSet(StringComparer.Ordinal);
        var next = request.PermissionCodes.ToHashSet(StringComparer.Ordinal);
        if (old.SetEquals(next)) return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, next.Order(StringComparer.Ordinal).ToArray()));
        db.RemoveRange(existing.Where(x => !next.Contains(x.ClaimValue!)));
        foreach (var code in next.Except(old)) db.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = code });
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync(cancellationToken);
        if (!await checks.HasRecoveryAdministratorAsync(cancellationToken)) return SecurityMutationChecks.Recovery;
        await audit.RecordAsync("Identity.RolePermissionsUpdated", "Role", role.Id,
            SecurityAuditWriter.Values("permissionCodes", old.Except(next).Order(StringComparer.Ordinal).ToArray()),
            SecurityAuditWriter.Values("permissionCodes", next.Except(old).Order(StringComparer.Ordinal).ToArray()), cancellationToken);
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, next.Order(StringComparer.Ordinal).ToArray()));
    }
}
