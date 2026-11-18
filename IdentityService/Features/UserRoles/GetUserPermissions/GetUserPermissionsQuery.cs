namespace IdentityService.Features.UserRoles.GetUserPermissions;

public sealed record GetUserPermissionsQuery(string UserId) : IRequest<Result<string[]>>;
