namespace IdentityService.Features.Auth.ForgetPassword;
public sealed record ForgetPasswordCommand(string Email) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(ForgetPasswordCommand);
}
