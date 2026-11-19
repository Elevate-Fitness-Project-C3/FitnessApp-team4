using Audit.Contracts.V1;
using MassTransit;
using Microsoft.Extensions.Options;

namespace AuditService.Messaging;

public sealed class AuditEventRecordedConsumer(AuditEventIngestor ingestor, IOptions<AuditMessagingOptions> options)
    : IConsumer<AuditEventRecordedV1>
{
    public async Task Consume(ConsumeContext<AuditEventRecordedV1> context)
    {
        var queue = context.ReceiveContext.InputAddress.AbsolutePath.Split('/').Last();
        var source = options.Value.Sources.SingleOrDefault(x => queue == $"audit-ingest-{x.SourceService}-v1")
            ?? throw new InvalidAuditEventException("Unregistered audit source endpoint.");
        await ingestor.IngestAsync(context.Message, source, context.CancellationToken);
    }
}
