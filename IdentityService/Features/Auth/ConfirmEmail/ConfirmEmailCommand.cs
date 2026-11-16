namespace IdentityService.Features.Auth.ConfirmEmail;
public sealed record ConfirmEmailCommand(string Email, string Code) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(ConfirmEmailCommand);
}
