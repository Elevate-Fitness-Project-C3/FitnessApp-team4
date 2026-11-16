namespace IdentityService.Security.Options;

public sealed class EmailConfirmationTokenOptions
{
    public const string SectionName = "EmailConfirmationToken";
    public int ExpirationMinutes { get; init; }
}