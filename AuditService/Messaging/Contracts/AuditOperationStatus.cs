using System.Text.Json.Serialization;

namespace Audit.Contracts.V1;

[JsonConverter(typeof(AuditEnumJsonConverter<AuditOperationStatus>))]
public enum AuditOperationStatus
{
    Committed = 1,
    Denied = 2,
    Failed = 3
}
