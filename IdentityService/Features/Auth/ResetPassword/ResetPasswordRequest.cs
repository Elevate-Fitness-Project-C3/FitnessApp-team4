namespace IdentityService.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword)
{
    public override string ToString() => nameof(ResetPasswordRequest);
}
