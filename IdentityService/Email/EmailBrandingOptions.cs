namespace IdentityService.Email;

public sealed class EmailBrandingOptions
{
    public string ApplicationName { get; init; } = "Identity Service";
    public string TeamName { get; init; } = "Support Team";
    public string ConfirmEmailSubject { get; init; } = "Confirm email";
    public string ResetPasswordSubject { get; init; } = "Reset password";
}
