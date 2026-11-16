using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.Login;
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
