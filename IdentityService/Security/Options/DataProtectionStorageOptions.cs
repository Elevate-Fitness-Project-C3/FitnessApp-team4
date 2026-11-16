namespace IdentityService.Security.Options;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";
    public string ApplicationName { get; init; } = string.Empty;
    public string KeysPath { get; init; } = string.Empty;
}