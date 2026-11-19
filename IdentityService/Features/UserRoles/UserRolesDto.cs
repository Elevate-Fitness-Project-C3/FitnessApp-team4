namespace IdentityService.Features.UserRoles;

public sealed record UserRolesDto(string UserId, string ConcurrencyToken, string[] RoleIds);
