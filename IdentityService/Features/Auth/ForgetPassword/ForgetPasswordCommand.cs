using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ForgetPassword;
public sealed record ForgetPasswordCommand(string Email) : IRequest<Result<Success>>;
