namespace IdentityService.Features.Roles.UpdateRolePermissions;

public sealed record UpdateRolePermissionsCommand(string RoleId, string[] PermissionCodes, string ConcurrencyToken) : IRequest<Result<RoleDto>>;
