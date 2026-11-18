namespace IdentityService.Features.Roles.RenameRole;

public sealed record RenameRoleRequest(string Name, string ConcurrencyToken);
