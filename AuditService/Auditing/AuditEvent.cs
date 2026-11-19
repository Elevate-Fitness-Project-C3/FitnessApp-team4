using Audit.Contracts.V1;

namespace AuditService.Auditing;

public sealed class AuditEvent
{
    public long Id { get; private set; }
    public required Guid EventId { get; init; }
    public required string SourceService { get; init; }
    public required string ApplicationId { get; init; }
    public string? TenantId { get; init; }
    public required AuditActorKind ActorKind { get; init; }
    public string? ActorUserId { get; init; }
    public string? ActorUserNameSnapshot { get; init; }
    public string? ActorSystemId { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public required DateTimeOffset IngestedAtUtc { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceName { get; init; }
    public string? UserAgent { get; init; }
    public required string CorrelationId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public required AuditOperationStatus Status { get; init; }
}
