using System.Net;
using System.Text.Json;
using Audit.Contracts.V1;

namespace AuditService.Messaging;

public sealed class InvalidAuditEventException(string message) : Exception(message);
public sealed class AuditEventConflictException() : Exception("Audit event identity was reused with different content.");

public static class AuditEventValidation
{
    private static readonly Dictionary<string, string[]> AllowedChanges = new(StringComparer.Ordinal)
    {
        ["Identity.RoleCreated"] = ["name"],
        ["Identity.RoleRenamed"] = ["name"],
        ["Identity.RoleDeleted"] = ["name"],
        ["Identity.RolePermissionsUpdated"] = ["permissionCodes"],
        ["Identity.RoleAssigned"] = ["roleId"],
        ["Identity.RoleRemoved"] = ["roleId"],
        ["Identity.UserStatusUpdated"] = ["isActive"],
        ["Identity.AuditGrantCreated"] = ["applicationId", "tenantId", "sourceService"],
        ["Identity.AuditGrantRemoved"] = ["applicationId", "tenantId", "sourceService"],
        ["Identity.SecurityBootstrap"] = ["roleId"],
        ["Identity.SecurityOperationDenied"] = [],
        ["Identity.SecurityOperationFailed"] = []
    };

    public static void Validate(AuditEventRecordedV1 value, AuditSourceOptions source)
    {
        Require(value.EventId != Guid.Empty && value.SourceService == source.SourceService
            && value.ApplicationId == source.ApplicationId, "Invalid event identity or source scope.");
        Text(value.SourceService, 64, true); Text(value.ApplicationId, 100, true);
        Text(value.TenantId, 128, false);
        Require(value.TenantId is null ? source.AllowApplicationEvents : source.TenantIds.Contains(value.TenantId, StringComparer.Ordinal), "Tenant scope is not allowed for this source.");
        Text(value.ActorUserId, 450, false); Text(value.ActorUserNameSnapshot, 256, false); Text(value.ActorSystemId, 128, false);
        Require(value.ActorKind switch
        {
            AuditActorKind.User => value.ActorUserId is not null && value.ActorSystemId is null,
            AuditActorKind.System => value.ActorSystemId is not null && value.ActorUserId is null && value.ActorUserNameSnapshot is null,
            AuditActorKind.Anonymous => value.ActorSystemId is null && value.ActorUserId is null && value.ActorUserNameSnapshot is null,
            _ => false
        }, "Invalid actor attribution.");
        Text(value.Action, 160, true); Text(value.EntityType, 128, true); Text(value.EntityId, 450, value.Status == AuditOperationStatus.Committed);
        Text(value.CorrelationId, 128, true); Text(value.DeviceName, 128, false); Text(value.UserAgent, 1024, false); Text(value.IpAddress, 45, false);
        Require(value.IpAddress is null || IPAddress.TryParse(value.IpAddress, out _), "Invalid IP metadata.");
        Require(value.TimestampUtc != default && value.TimestampUtc.Offset == TimeSpan.Zero && value.TimestampUtc <= DateTimeOffset.UtcNow.AddMinutes(5), "Invalid UTC event time.");
        Require(Enum.IsDefined(value.Status), "Invalid operation status.");
        Require(AllowedChanges.TryGetValue(value.Action, out var keys), "Unregistered audit action.");
        Require(value.SourceService == "identity-service", "No action catalog is registered for this source.");
        var expectedEntity = value.Action switch
        {
            "Identity.RoleAssigned" or "Identity.RoleRemoved" or "Identity.UserStatusUpdated" or "Identity.SecurityBootstrap" => "User",
            "Identity.AuditGrantCreated" or "Identity.AuditGrantRemoved" => "AuditReportGrant",
            "Identity.SecurityOperationDenied" or "Identity.SecurityOperationFailed" => "SecurityOperation",
            _ => "Role"
        };
        Require(value.EntityType == expectedEntity, "Action does not match the affected entity type.");
        foreach (var changes in new[] { value.OldValues, value.NewValues })
        {
            if (changes is null) continue;
            Require(changes.Count <= 16, "Too many change fields.");
            foreach (var (key, json) in changes)
            {
                Require(keys!.Contains(key, StringComparer.Ordinal), "Unapproved audit change field.");
                Require(json.ValueKind is not (JsonValueKind.Object or JsonValueKind.Undefined), "Nested audit objects are not allowed.");
                if (key == "permissionCodes")
                    Require(json.ValueKind == JsonValueKind.Array && json.GetArrayLength() <= 500 && json.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String && x.GetString()!.Length <= 160), "Invalid permission change set.");
                else if (key == "isActive") Require(json.ValueKind is JsonValueKind.True or JsonValueKind.False, "Invalid status change value.");
                else Require(json.ValueKind == JsonValueKind.Null || json.ValueKind == JsonValueKind.String && json.GetString()!.Length <= 450, "Invalid change value type.");
            }
        }
        Require(JsonSerializer.SerializeToUtf8Bytes(value).Length <= 65536, "Audit event exceeds the permitted size.");
    }

    private static void Text(string? value, int max, bool required)
        => Require(value is null ? !required : value.Length > 0 && value.Length <= max && value.Trim() == value && !value.Any(char.IsControl), "Invalid audit field length or characters.");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidAuditEventException(message);
    }
}
