using System.Text.Json;
using System.Text.Json.Serialization;

namespace Audit.Contracts.V1;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public sealed record AuditEventRecordedV1
{
    [JsonPropertyName("eventId")]
    public required Guid EventId { get; init; }

    [JsonPropertyName("sourceService")]
    public required string SourceService { get; init; }

    [JsonPropertyName("applicationId")]
    public required string ApplicationId { get; init; }

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

    [JsonPropertyName("actorKind")]
    public required AuditActorKind ActorKind { get; init; }

    [JsonPropertyName("actorUserId")]
    public string? ActorUserId { get; init; }

    [JsonPropertyName("actorUserNameSnapshot")]
    public string? ActorUserNameSnapshot { get; init; }

    [JsonPropertyName("actorSystemId")]
    public string? ActorSystemId { get; init; }

    [JsonPropertyName("action")]
    public required string Action { get; init; }

    [JsonPropertyName("entityType")]
    public required string EntityType { get; init; }

    [JsonPropertyName("entityId")]
    public string? EntityId { get; init; }

    [JsonPropertyName("timestampUtc")]
    public required DateTimeOffset TimestampUtc { get; init; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; init; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }

    [JsonPropertyName("correlationId")]
    public required string CorrelationId { get; init; }

    [JsonPropertyName("oldValues")]
    public IReadOnlyDictionary<string, JsonElement>? OldValues { get; init; }

    [JsonPropertyName("newValues")]
    public IReadOnlyDictionary<string, JsonElement>? NewValues { get; init; }

    [JsonPropertyName("status")]
    public required AuditOperationStatus Status { get; init; }
}
