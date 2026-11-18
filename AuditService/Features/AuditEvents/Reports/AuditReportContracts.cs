using Audit.Contracts.V1;

namespace AuditService.Features.AuditEvents.Reports;

public sealed record AuditEventSearchRequest
{
    public DateTimeOffset FromUtc { get; init; }
    public DateTimeOffset ToUtc { get; init; }
    public string? ApplicationId { get; init; }
    public string? TenantId { get; init; }
    public string? SourceService { get; init; }
    public string? ActorUserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? DeviceName { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public AuditOperationStatus? Status { get; init; }
    public int PageSize { get; init; } = 50;
    public string Sort { get; init; } = "timestampUtc";
    public bool Descending { get; init; } = true;
    public string? Cursor { get; init; }
}

public sealed record AuditEventSummaryDto(Guid EventId, string SourceService, string ApplicationId, string? TenantId,
    string? ActorUserId, string? ActorUserNameSnapshot, string Action, string EntityType, string? EntityId,
    DateTimeOffset TimestampUtc, DateTimeOffset IngestedAtUtc, string CorrelationId, AuditOperationStatus Status);
public sealed record AuditEventPageDto(IReadOnlyList<AuditEventSummaryDto> Items, string? NextCursor, bool HasMore);
