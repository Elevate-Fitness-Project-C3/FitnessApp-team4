namespace IdentityService.Security.Options;


public sealed class RsaKeyOptions
{
    public const string SectionName = "RsaKeys";
    public string PrivateKeyPath { get; init; } = string.Empty;
    public string PublicKeyPath { get; init; } = string.Empty;
}