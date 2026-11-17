namespace IdentityService.Controllers;
public sealed record LoginRequest(string Email, string Password)
{
    public override string ToString() => nameof(LoginRequest);
}
public sealed record LogoutRequest(string Token, string RefreshToken)
{
    public override string ToString() => nameof(LogoutRequest);
}
public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword)
{
    public override string ToString() => nameof(ResetPasswordRequest);
}
public sealed record RegisterUserRequest(string FirstName, string LastName, string Email, string Password)
{
    public override string ToString() => nameof(RegisterUserRequest);
}
public sealed record ForgetPasswordRequest(string Email)
{
    public override string ToString() => nameof(ForgetPasswordRequest);
}
public sealed record RefreshTokenRequest(string Token, string RefreshToken)
{
    public override string ToString() => nameof(RefreshTokenRequest);
}
public sealed record ResendConfirmationEmailRequest(string Email)
{
    public override string ToString() => nameof(ResendConfirmationEmailRequest);
}
public sealed record ConfirmEmailRequest(string Email, string Code)
{
    public override string ToString() => nameof(ConfirmEmailRequest);
}
