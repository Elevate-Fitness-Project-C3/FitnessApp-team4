namespace IdentityService.Security.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; }
    public int MaxActiveRefreshTokens { get; init; } = 10;
    public int RefreshTokenExpirationDays { get; init; }
}
