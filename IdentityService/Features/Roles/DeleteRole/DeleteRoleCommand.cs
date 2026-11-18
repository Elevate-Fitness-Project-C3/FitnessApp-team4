namespace IdentityService.Features.Roles.DeleteRole;

public sealed record DeleteRoleCommand(string RoleId, string ConcurrencyToken) : IRequest<Result<Success>>;
