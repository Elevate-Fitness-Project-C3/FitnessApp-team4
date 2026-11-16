using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.Logout;
public sealed record LogoutCommand(string Token, string RefreshToken) : IRequest<Result<Success>>;
