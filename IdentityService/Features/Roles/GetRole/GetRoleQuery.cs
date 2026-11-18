namespace IdentityService.Features.Roles.GetRole;

public sealed record GetRoleQuery(string RoleId) : IRequest<Result<RoleDto>>;
