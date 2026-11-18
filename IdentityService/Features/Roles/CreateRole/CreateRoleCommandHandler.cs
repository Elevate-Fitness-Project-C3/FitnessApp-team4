using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles.CreateRole;

public sealed class CreateRoleCommandHandler(RoleManager<IdentityRole> roles, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Roles.Create", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<RoleDto>> ApplyAsync(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new IdentityRole(request.Name);
        var result = await roles.CreateAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        await audit.RecordAsync("Identity.RoleCreated", "Role", role.Id, null, SecurityAuditWriter.Values("name", role.Name), cancellationToken);
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, []));
    }
}
