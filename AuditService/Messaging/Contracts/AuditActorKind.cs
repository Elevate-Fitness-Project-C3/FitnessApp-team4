using System.Text.Json.Serialization;

namespace Audit.Contracts.V1;

[JsonConverter(typeof(AuditEnumJsonConverter<AuditActorKind>))]
public enum AuditActorKind
{
    User = 1,
    System = 2,
    Anonymous = 3
}
