namespace IdentityService.Features.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name) : IRequest<Result<RoleDto>>;
