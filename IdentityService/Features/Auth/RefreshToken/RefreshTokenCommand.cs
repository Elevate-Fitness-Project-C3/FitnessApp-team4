using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.RefreshToken;
public sealed record RefreshTokenCommand(string Token, string RefreshToken) : IRequest<Result<AuthResponse>>;
