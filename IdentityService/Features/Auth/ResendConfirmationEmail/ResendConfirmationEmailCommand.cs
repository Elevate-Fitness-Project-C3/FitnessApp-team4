namespace IdentityService.Features.Auth.ResendConfirmationEmail;
public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(ResendConfirmationEmailCommand);
}
