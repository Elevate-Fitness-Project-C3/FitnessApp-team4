namespace IdentityService.Features.Auth.Login;

public sealed record LoginRequest(string Email, string Password)
{
    public override string ToString() => nameof(LoginRequest);
}
