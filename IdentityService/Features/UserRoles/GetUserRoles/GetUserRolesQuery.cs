
namespace IdentityService.Features.UserRoles.GetUserRoles;

public sealed record GetUserRolesQuery(string UserId) : IRequest<Result<UserRolesDto>>;
