namespace IdentityService.Features.UserRoles.SetUserStatus;

public sealed record UserStatusRequest(bool IsActive, string ConcurrencyToken);
