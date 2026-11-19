namespace IdentityService.Security.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";
    public int ExpirationMinutes { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;
    public int ResendCooldownSeconds { get; init; } = 60;
}
