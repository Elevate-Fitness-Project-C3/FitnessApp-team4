namespace IdentityService.Features.Roles;

public sealed record RoleDto(string Id, string Name, string ConcurrencyToken, string[] PermissionCodes);
public sealed record UserRolesDto(string UserId, string ConcurrencyToken, string[] RoleIds);
public sealed record PageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record RenameRoleRequest(string Name, string ConcurrencyToken);
public sealed record UpdateRolePermissionsRequest(string[] PermissionCodes, string ConcurrencyToken);
public sealed record UserRoleRequest(string ConcurrencyToken);
public sealed record UserStatusRequest(bool IsActive, string ConcurrencyToken);
