using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.UserRoles.SetUserRole;

public sealed class SetUserRoleCommandHandler(AuthDbContext db, UserManager<ApplicationUser> users, SecurityMutationChecks checks, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<SetUserRoleCommand, Result<UserRolesDto>>
{
    public async Task<Result<UserRolesDto>> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        var permission = request.Assign ? "Identity.Users.Roles.Assign" : "Identity.Users.Roles.Remove";
        var result = await orchestrator.ExecuteAsync(permission, () => ApplyAsync(request, cancellationToken), cancellationToken);
        if (!result.IsSuccess)
            return Result<UserRolesDto>.FromErrors(result.Errors);

        var user = result.Value;
        var roles = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        return Result<UserRolesDto>.Success(new(user.Id, user.ConcurrencyStamp!, roles));
    }

    private async Task<Result<ApplicationUser>> ApplyAsync(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        await UserRowLock.AcquireAsync(db, request.UserId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (user is null || role is null) return SecurityMutationChecks.Missing;
        if (user.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (!await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        var assigned = await db.Set<IdentityUserRole<string>>().AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken);
        if (assigned != request.Assign)
        {
            var result = request.Assign ? await users.AddToRoleAsync(user, role.Name!) : await users.RemoveFromRoleAsync(user, role.Name!);
            if (!result.Succeeded) return IdentityErrorMapper.ToResult<ApplicationUser>(result);
            if (!await checks.HasRecoveryAdministratorAsync(cancellationToken)) return SecurityMutationChecks.Recovery;
            await audit.RecordAsync(request.Assign ? AuditActions.RoleAssigned : AuditActions.RoleRemoved, "User", user.Id,
                request.Assign ? null : SecurityAuditWriter.Values("roleId", role.Id),
                request.Assign ? SecurityAuditWriter.Values("roleId", role.Id) : null, cancellationToken);
        }
        return user;
    }
}
