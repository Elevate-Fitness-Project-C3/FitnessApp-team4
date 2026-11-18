namespace IdentityService.Features.Permissions.GetPermissionTree;

public sealed record PermissionNode(string Code, string DisplayNameAr, string Kind, int DisplayOrder, bool IsActive, bool IsSelected, IReadOnlyList<PermissionNode> Children);
