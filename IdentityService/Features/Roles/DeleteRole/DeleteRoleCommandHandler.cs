using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler(AuthDbContext db, RoleManager<IdentityRole> roles, SecurityMutationChecks checks, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<DeleteRoleCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Roles.Delete", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<Success>> ApplyAsync(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roles.FindByIdAsync(request.RoleId);
        if (role is null) return SecurityMutationChecks.Missing;
        if (role.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (SecurityMutationChecks.IsDefaultRole(role) || !await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        if (await db.Set<IdentityUserRole<string>>().AnyAsync(x => x.RoleId == role.Id, cancellationToken))
            return Error.Conflict("Role.Assigned", "Remove role assignments before deleting the role.");
        var result = await roles.DeleteAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<Success>(result);
        await audit.RecordAsync(AuditActions.RoleDeleted, "Role", role.Id, SecurityAuditWriter.Values("name", role.Name), null, cancellationToken);
        return Result.Success;
    }
}
