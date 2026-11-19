namespace IdentityService.Features.Auth.ConfirmEmail;
public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").Length(6);
    }
}
