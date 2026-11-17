namespace IdentityService.Features.Auth;
internal static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials.");
    public static readonly Error EmailNotConfirmed = Error.Forbidden("Auth.EmailNotConfirmed", "Email is not confirmed.");
    public static readonly Error InvalidCode = Error.Validation("Auth.InvalidCode", "Invalid or expired code.");
    public static readonly Error InvalidAccessToken = Error.Unauthorized("Auth.InvalidAccessToken", "Invalid access token.");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid refresh token.");
}
