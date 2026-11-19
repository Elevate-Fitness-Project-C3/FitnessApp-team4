namespace IdentityService.Features.UserRoles.SetUserStatus;

public sealed record SetUserStatusCommand(string UserId, bool IsActive, string ConcurrencyToken) : IRequest<Result<Success>>;
