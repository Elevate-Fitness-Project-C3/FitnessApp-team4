using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ResendConfirmationEmail;
public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Result<Success>>;
