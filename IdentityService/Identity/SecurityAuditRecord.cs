namespace IdentityService.Identity;

public sealed class SecurityAuditRecord
{
    public Guid EventId { get; init; }
    public string? ActorUserId { get; init; }
    public string Action { get; init; } = "";
    public string EntityType { get; init; } = "";
    public string? EntityId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public string EventPayload { get; init; } = "";
}

public sealed class AuditReportGrant
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string ApplicationId { get; set; } = "";
    public string? TenantId { get; set; }
    public string SourceService { get; set; } = "";
}
