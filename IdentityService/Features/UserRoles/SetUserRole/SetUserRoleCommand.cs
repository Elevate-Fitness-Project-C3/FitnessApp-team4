
namespace IdentityService.Features.UserRoles.SetUserRole;

public sealed record SetUserRoleCommand(string UserId, string RoleId, bool Assign, string ConcurrencyToken) : IRequest<Result<UserRolesDto>>;
