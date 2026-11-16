namespace IdentityService.Features.Auth.Login;
public sealed record AuthResponse(string UserId, string Email, string FirstName, string LastName, string Token, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiration);
