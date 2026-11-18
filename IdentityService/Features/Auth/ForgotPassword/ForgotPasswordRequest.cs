namespace IdentityService.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordRequest(string Email)
{
    public override string ToString() => nameof(ForgotPasswordRequest);
}
