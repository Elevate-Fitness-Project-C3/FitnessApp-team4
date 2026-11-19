namespace IdentityService.Features.Roles.RenameRole;

public sealed record RenameRoleCommand(string RoleId, string Name, string ConcurrencyToken) : IRequest<Result<RoleDto>>;
