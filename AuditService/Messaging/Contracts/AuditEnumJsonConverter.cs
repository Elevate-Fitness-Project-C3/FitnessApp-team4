using System.Text.Json.Serialization;

namespace Audit.Contracts.V1;

public sealed class AuditEnumJsonConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
