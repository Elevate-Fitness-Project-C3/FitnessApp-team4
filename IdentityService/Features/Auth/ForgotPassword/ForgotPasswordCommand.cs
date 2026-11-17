namespace IdentityService.Features.Auth.ForgotPassword;
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(ForgotPasswordCommand);
}
