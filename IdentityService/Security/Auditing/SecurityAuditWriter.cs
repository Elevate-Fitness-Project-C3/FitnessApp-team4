using System.Text.Json;
using Audit.Contracts.V1;
using MassTransit;

namespace IdentityService.Security.Auditing;

public sealed class SecurityAuditWriter(AuthDbContext db, AuditActorContext actor, IServiceProvider services)
{
    public async Task RecordAsync(string action, string entityType, string? entityId,
        IReadOnlyDictionary<string, JsonElement>? oldValues, IReadOnlyDictionary<string, JsonElement>? newValues,
        CancellationToken cancellationToken, AuditOperationStatus status = AuditOperationStatus.Committed)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Security audit requires a local transaction.");
        var endpointProvider = services.GetService<ISendEndpointProvider>()
            ?? throw new InvalidOperationException("Enable and provision audit messaging before executing security mutations.");
        var user = actor.UserId is null ? null : await db.Users.AsNoTracking().SingleAsync(x => x.Id == actor.UserId, cancellationToken);
        var auditEvent = new AuditEventRecordedV1
        {
            EventId = Guid.NewGuid(), SourceService = "identity-service", ApplicationId = "fitness", TenantId = null,
            ActorKind = user is not null ? AuditActorKind.User : actor.IsHttpRequest ? AuditActorKind.Anonymous : AuditActorKind.System,
            ActorUserId = user?.Id, ActorUserNameSnapshot = user?.UserName,
            ActorSystemId = user is null && !actor.IsHttpRequest ? "identity-service:security-bootstrap" : null,
            Action = action, EntityType = entityType, EntityId = entityId, TimestampUtc = DateTimeOffset.UtcNow,
            IpAddress = actor.IpAddress, UserAgent = actor.UserAgent, DeviceName = null,
            CorrelationId = actor.CorrelationId, OldValues = oldValues, NewValues = newValues,
            Status = status
        };
        db.Set<SecurityAuditRecord>().Add(new SecurityAuditRecord
        {
            EventId = auditEvent.EventId, ActorUserId = auditEvent.ActorUserId, Action = action,
            EntityType = entityType, EntityId = entityId, TimestampUtc = auditEvent.TimestampUtc,
            EventPayload = JsonSerializer.Serialize(auditEvent)
        });
        var endpoint = await endpointProvider.GetSendEndpoint(new Uri("exchange:audit.identity-service.v1"));
        await endpoint.Send(auditEvent, context => { context.MessageId = auditEvent.EventId; }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static IReadOnlyDictionary<string, JsonElement> Values(string key, object? value)
        => new Dictionary<string, JsonElement> { [key] = JsonSerializer.SerializeToElement(value) };
}
