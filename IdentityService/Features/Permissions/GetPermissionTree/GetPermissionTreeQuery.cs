namespace IdentityService.Features.Permissions.GetPermissionTree;

public sealed record GetPermissionTreeQuery(string? RoleId) : IRequest<Result<PermissionTreeDto>>;
