namespace IdentityService.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailRequest(string Email, string Code)
{
    public override string ToString() => nameof(ConfirmEmailRequest);
}
