using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ConfirmEmail;
public sealed record ConfirmEmailCommand(string Email, string Code) : IRequest<Result<Success>>;
