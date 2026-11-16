namespace IdentityService.Features.Auth.Logout;
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
