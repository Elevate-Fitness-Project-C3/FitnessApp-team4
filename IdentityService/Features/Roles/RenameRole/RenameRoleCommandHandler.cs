using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles.RenameRole;

public sealed class RenameRoleCommandHandler(RoleManager<IdentityRole> roles, SecurityMutationChecks checks, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator, EffectivePermissionResolver permissions)
    : IRequestHandler<RenameRoleCommand, Result<RoleDto>>
{
    public Task<Result<RoleDto>> Handle(RenameRoleCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Roles.Rename", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<RoleDto>> ApplyAsync(RenameRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roles.FindByIdAsync(request.RoleId);
        if (role is null) return SecurityMutationChecks.Missing;
        if (role.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (SecurityMutationChecks.IsDefaultRole(role) || !await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        var grants = (await permissions.RolePermissionsAsync(role.Id, cancellationToken)).Order(StringComparer.Ordinal).ToArray();
        if (role.Name == request.Name) return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, grants));
        var oldName = role.Name;
        var result = await roles.SetRoleNameAsync(role, request.Name);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        result = await roles.UpdateAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        await audit.RecordAsync("Identity.RoleRenamed", "Role", role.Id, SecurityAuditWriter.Values("name", oldName), SecurityAuditWriter.Values("name", role.Name), cancellationToken);
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, grants));
    }
}
