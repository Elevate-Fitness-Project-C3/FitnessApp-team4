namespace IdentityService.Features.Auth.ResendConfirmationEmail;

public sealed record ResendConfirmationEmailRequest(string Email)
{
    public override string ToString() => nameof(ResendConfirmationEmailRequest);
}
