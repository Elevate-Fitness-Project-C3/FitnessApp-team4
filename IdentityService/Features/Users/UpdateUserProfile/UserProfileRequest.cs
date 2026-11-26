namespace IdentityService.Features.Users.UpdateUserProfile;

public sealed record UserProfileRequest(string FirstName, string LastName, string? PhoneNumber, string ConcurrencyToken);
