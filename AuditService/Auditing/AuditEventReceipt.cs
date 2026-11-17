namespace AuditService.Auditing;

public sealed class AuditEventReceipt
{
    public required string SourceService { get; init; }
    public required Guid EventId { get; init; }
    public required byte[] PayloadHash { get; init; }
    public required DateTimeOffset FirstIngestedAtUtc { get; init; }
}
