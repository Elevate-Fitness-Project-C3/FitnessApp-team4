using System.Net;
using System.Text.Json;
using Audit.Contracts.V1;

namespace AuditService.Messaging;

public sealed class InvalidAuditEventException(string message) : Exception(message);
public sealed class AuditEventConflictException() : Exception("Audit event identity was reused with different content.");

public static class AuditEventValidation
{
    private const string IdentitySource = "identity-service";

    private static readonly Dictionary<string, (string EntityType, string[] Fields)> IdentityActions = new(StringComparer.Ordinal)
    {
        ["Identity.RoleCreated"] = ("Role", ["name"]),
        ["Identity.RoleRenamed"] = ("Role", ["name"]),
        ["Identity.RoleDeleted"] = ("Role", ["name"]),
        ["Identity.RolePermissionsUpdated"] = ("Role", ["permissionCodes"]),
        ["Identity.RoleAssigned"] = ("User", ["roleId"]),
        ["Identity.RoleRemoved"] = ("User", ["roleId"]),
        ["Identity.UserStatusUpdated"] = ("User", ["isActive"]),
        ["Identity.AuditGrantCreated"] = ("AuditReportGrant", ["applicationId", "tenantId", "sourceService"]),
        ["Identity.AuditGrantRemoved"] = ("AuditReportGrant", ["applicationId", "tenantId", "sourceService"]),
        ["Identity.SecurityBootstrap"] = ("User", ["roleId"]),
        ["Identity.SecurityOperationDenied"] = ("SecurityOperation", []),
        ["Identity.SecurityOperationFailed"] = ("SecurityOperation", [])
    };

    internal static bool ValidProfile(AuditSourceOptions source) =>
        ValidIdentifier(source.ApplicationId, 100)
        && source.TenantIds.All(tenant => ValidText(tenant, 128, true))
        && (source.AllowApplicationEvents || source.TenantIds.Length > 0)
        && (source.SourceService == IdentitySource
            ? source.Actions.Length == 0
            : source.Actions.Length > 0
                && source.Actions.Select(x => x.Action).Distinct(StringComparer.Ordinal).Count() == source.Actions.Length
                && source.Actions.All(x => ValidIdentifier(x.Action, 160) && ValidIdentifier(x.EntityType, 128)
                    && x.ChangeFields.Count <= 16 && x.ChangeFields.All(field =>
                        ValidIdentifier(field.Key, 64) && field.Value is "boolean" or "integer" or "guid")));

    public static void Validate(AuditEventRecordedV1 value, AuditSourceOptions source)
    {
        ValidateContract(value);
        Require(value.SourceService == source.SourceService && value.ApplicationId == source.ApplicationId, "Invalid source scope.");
        Require(value.TenantId is null ? source.AllowApplicationEvents : source.TenantIds.Contains(value.TenantId, StringComparer.Ordinal), "Tenant scope is not allowed for this source.");
        ValidateAction(value, source);
        Require(JsonSerializer.SerializeToUtf8Bytes(value).Length <= 65536, "Audit event exceeds the permitted size.");
    }

    private static void ValidateContract(AuditEventRecordedV1 value)
    {
        Require(value.EventId != Guid.Empty, "Invalid event identity.");
        Require(AuditMessagingConfiguration.ValidSource(value.SourceService) && ValidIdentifier(value.ApplicationId, 100), "Invalid source or application identifier.");
        Text(value.TenantId, 128, false);
        Text(value.ActorUserId, 450, false); Text(value.ActorUserNameSnapshot, 256, false); Text(value.ActorSystemId, 128, false);
        Require(value.ActorKind switch
        {
            AuditActorKind.User => value.ActorUserId is not null && value.ActorSystemId is null,
            AuditActorKind.System => value.ActorSystemId is not null && value.ActorUserId is null && value.ActorUserNameSnapshot is null,
            AuditActorKind.Anonymous => value.ActorSystemId is null && value.ActorUserId is null && value.ActorUserNameSnapshot is null,
            _ => false
        }, "Invalid actor attribution.");
        Require(ValidIdentifier(value.Action, 160) && ValidIdentifier(value.EntityType, 128), "Invalid action or entity type.");
        Text(value.EntityId, 450, value.Status == AuditOperationStatus.Committed);
        Require(ValidIdentifier(value.CorrelationId, 128), "Invalid correlation identifier.");
        Text(value.DeviceName, 128, false); Text(value.UserAgent, 1024, false); Text(value.IpAddress, 45, false);
        Require(value.IpAddress is null || IPAddress.TryParse(value.IpAddress, out _), "Invalid IP metadata.");
        Require(value.TimestampUtc != default && value.TimestampUtc.Offset == TimeSpan.Zero && value.TimestampUtc <= DateTimeOffset.UtcNow.AddMinutes(5), "Invalid UTC event time.");
        Require(Enum.IsDefined(value.Status), "Invalid operation status.");
    }

    private static void ValidateAction(AuditEventRecordedV1 value, AuditSourceOptions source)
    {
        var isIdentity = source.SourceService == IdentitySource;
        IdentityActions.TryGetValue(value.Action, out var identity);
        var configured = isIdentity ? null : source.Actions.SingleOrDefault(action => action.Action == value.Action);
        var expectedEntity = isIdentity ? identity.EntityType : configured?.EntityType;
        Require(expectedEntity is not null, "Unregistered audit action.");
        Require(value.EntityType == expectedEntity, "Action does not match the affected entity type.");
        ValidateChanges(value.OldValues);
        ValidateChanges(value.NewValues);

        void ValidateChanges(IReadOnlyDictionary<string, JsonElement>? changes)
        {
            if (changes is null) return;
            Require(changes.Count <= 16, "Too many change fields.");
            foreach (var (key, json) in changes)
            {
                if (isIdentity)
                {
                    Require(identity.Fields.Contains(key, StringComparer.Ordinal), "Unapproved audit change field.");
                    Require(ValidIdentityChange(key, json), "Invalid Identity audit change.");
                }
                else
                {
                    Require(configured!.ChangeFields.TryGetValue(key, out var type), "Unapproved audit change field.");
                    Require(type switch
                    {
                        "boolean" => json.ValueKind is JsonValueKind.True or JsonValueKind.False,
                        "integer" => json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out _),
                        "guid" => json.ValueKind == JsonValueKind.String && json.GetString() is { Length: 36 } id && Guid.TryParseExact(id, "D", out _),
                        _ => false
                    }, "Invalid source audit change.");
                }
            }
        }
    }

    private static bool ValidIdentityChange(string key, JsonElement json) => key switch
    {
        "permissionCodes" => json.ValueKind == JsonValueKind.Array && json.GetArrayLength() <= 500
            && json.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String && x.GetString()!.Length <= 160),
        "isActive" => json.ValueKind is JsonValueKind.True or JsonValueKind.False,
        _ => json.ValueKind == JsonValueKind.Null || json.ValueKind == JsonValueKind.String && json.GetString()!.Length <= 450
    };

    internal static bool ValidIdentifier(string? value, int max) =>
        value is not null && ValidText(value, max, true) && value.All(c => c is >= ' ' and <= '~');

    internal static bool ValidText(string? value, int max, bool required) =>
        value is null ? !required : value.Length > 0 && value.Length <= max && value.Trim() == value && !value.Any(char.IsControl);

    private static void Text(string? value, int max, bool required)
        => Require(ValidText(value, max, required), "Invalid audit field length or characters.");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidAuditEventException(message);
    }
}
