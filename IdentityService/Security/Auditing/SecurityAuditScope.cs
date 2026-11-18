namespace IdentityService.Security.Auditing;

public sealed class SecurityAuditScope(string applicationId)
{
    public const string SourceService = "identity-service";
    public string ApplicationId { get; } = applicationId;

    public static SecurityAuditScope FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["Messaging:ApplicationId"];
        if (value is null && !configuration.GetValue<bool>("Messaging:Enabled")) return new("identity");
        if (value is null || value.Length is < 1 or > 100 || value.Trim() != value
            || value.Any(c => c is < ' ' or > '~'))
            throw new InvalidOperationException("Messaging:ApplicationId must be a valid audit application identifier.");
        return new(value);
    }
}
