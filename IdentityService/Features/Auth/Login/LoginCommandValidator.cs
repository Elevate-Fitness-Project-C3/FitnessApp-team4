namespace IdentityService.Features.Auth.Login;
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
