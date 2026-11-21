using System.Security.Cryptography;
using System.Text.Json;
using Audit.Contracts.V1;
using AuditService.Auditing;
using AuditService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Messaging;

public sealed class AuditEventIngestor(AuditDbContext db)
{
    public async Task IngestAsync(AuditEventRecordedV1 value, AuditSourceOptions source, CancellationToken cancellationToken)
    {
        AuditEventValidation.Validate(value, source);
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Audit ingestion requires the EF consumer outbox transaction.");
        var hash = AuditPayloadHasher.Compute(value);
        // Serialize only competing deliveries of the same event, including different transport MessageIds.
        var resource = "audit:" + Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.SourceService + ":" + value.EventId.ToString("D"))));
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=4000;
            IF @result < 0 THROW 51001, 'Audit event lock could not be acquired.', 1;
            """, cancellationToken);
        var receipt = await db.AuditEventReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.SourceService == value.SourceService && x.EventId == value.EventId, cancellationToken);
        if (receipt is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(receipt.PayloadHash, hash)) throw new AuditEventConflictException();
            return;
        }
        var now = DateTimeOffset.UtcNow;
        db.AuditEventReceipts.Add(new AuditEventReceipt { SourceService = value.SourceService, EventId = value.EventId, PayloadHash = hash, FirstIngestedAtUtc = now });
        db.AuditEvents.Add(new AuditEvent
        {
            EventId = value.EventId, SourceService = value.SourceService, ApplicationId = value.ApplicationId,
            TenantId = value.TenantId, ActorKind = value.ActorKind, ActorUserId = value.ActorUserId,
            ActorUserNameSnapshot = value.ActorUserNameSnapshot, ActorSystemId = value.ActorSystemId,
            Action = value.Action, EntityType = value.EntityType, EntityId = value.EntityId,
            TimestampUtc = value.TimestampUtc, IngestedAtUtc = now, IpAddress = value.IpAddress,
            DeviceName = value.DeviceName, UserAgent = value.UserAgent, CorrelationId = value.CorrelationId,
            OldValues = value.OldValues is null ? null : JsonSerializer.Serialize(value.OldValues),
            NewValues = value.NewValues is null ? null : JsonSerializer.Serialize(value.NewValues), Status = value.Status
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
