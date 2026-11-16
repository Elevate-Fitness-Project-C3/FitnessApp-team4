namespace IdentityService.Features.Auth.Login;
public sealed record AuthResponse(string UserId, string Email, string FirstName, string LastName, string Token, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiration)
{
    internal static AuthResponse From(ApplicationUser user, IdentityService.Security.Tokens.IssuedTokens tokens) =>
        new(user.Id, user.Email!, user.FirstName, user.LastName, tokens.Token, tokens.ExpiresIn, tokens.RefreshToken, tokens.RefreshTokenExpiration);
    public override string ToString() => nameof(AuthResponse);
}
