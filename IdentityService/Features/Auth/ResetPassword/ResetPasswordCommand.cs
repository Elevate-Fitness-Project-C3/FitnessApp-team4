namespace IdentityService.Features.Auth.ResetPassword;
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(ResetPasswordCommand);
}
