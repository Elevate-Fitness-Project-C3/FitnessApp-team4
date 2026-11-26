using IdentityService.Features.Users.ListUsers;

namespace IdentityService.Features.Users.UpdateOwnProfile;

public sealed record UpdateOwnProfileCommand(string FirstName, string LastName, string? PhoneNumber, string ConcurrencyToken) : IRequest<Result<UserDto>>;
