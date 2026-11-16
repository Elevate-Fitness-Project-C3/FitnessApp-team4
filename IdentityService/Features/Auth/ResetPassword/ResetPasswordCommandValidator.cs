namespace IdentityService.Features.Auth.ResetPassword;
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
    }
}
