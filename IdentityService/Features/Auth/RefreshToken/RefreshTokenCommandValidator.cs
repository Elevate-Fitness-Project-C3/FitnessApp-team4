namespace IdentityService.Features.Auth.RefreshToken;
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
