namespace IdentityService.Features.Permissions.GetPermissionTree;

public sealed record PermissionTreeDto(string? RoleId, string? ConcurrencyToken, IReadOnlyList<PermissionNode> Nodes);
