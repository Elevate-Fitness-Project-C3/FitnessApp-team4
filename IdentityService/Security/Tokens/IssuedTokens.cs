namespace IdentityService.Security.Tokens;
public sealed record IssuedTokens(string Token, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiration)
{
    public override string ToString() => nameof(IssuedTokens);
}
