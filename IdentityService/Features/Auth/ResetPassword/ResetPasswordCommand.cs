using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ResetPassword;
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result<Success>>;
