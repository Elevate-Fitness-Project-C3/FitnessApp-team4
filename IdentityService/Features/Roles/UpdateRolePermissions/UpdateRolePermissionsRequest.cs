namespace IdentityService.Features.Roles.UpdateRolePermissions;

public sealed record UpdateRolePermissionsRequest(string[] PermissionCodes, string ConcurrencyToken);
