namespace IdentityService.Features.Roles;

public sealed record RoleDto(string Id, string Name, string ConcurrencyToken, string[] PermissionCodes);
