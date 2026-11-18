namespace IdentityService.Features.Auth.Logout;

public sealed record LogoutRequest(string Token, string RefreshToken)
{
    public override string ToString() => nameof(LogoutRequest);
}
