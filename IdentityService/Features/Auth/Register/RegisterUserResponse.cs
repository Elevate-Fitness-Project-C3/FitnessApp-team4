namespace IdentityService.Features.Auth.Register;

public sealed record RegisterUserResponse(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerificationRequired,
    bool EmailSent = false
);
