namespace AuditService.Messaging;

public sealed class AuditMessagingOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "audit";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseTls { get; set; } = true;
    public AuditSourceOptions[] Sources { get; set; } = [];
}

public sealed class AuditSourceOptions
{
    public string SourceService { get; set; } = "";
    public string ApplicationId { get; set; } = "";
    public bool AllowApplicationEvents { get; set; }
    public string[] TenantIds { get; set; } = [];
}
