namespace IdentityService.Features.Auth.Register;

public sealed record RegisterUserRequest(string FirstName, string LastName, string Email, string Password)
{
    public override string ToString() => nameof(RegisterUserRequest);
}
