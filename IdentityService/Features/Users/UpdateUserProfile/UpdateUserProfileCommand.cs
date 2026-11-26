using IdentityService.Features.Users.ListUsers;

namespace IdentityService.Features.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(string UserId, string FirstName, string LastName, string? PhoneNumber, string ConcurrencyToken) : IRequest<Result<UserDto>>;
