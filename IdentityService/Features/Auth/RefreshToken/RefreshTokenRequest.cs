namespace IdentityService.Features.Auth.RefreshToken;

public sealed record RefreshTokenRequest(string Token, string RefreshToken)
{
    public override string ToString() => nameof(RefreshTokenRequest);
}
