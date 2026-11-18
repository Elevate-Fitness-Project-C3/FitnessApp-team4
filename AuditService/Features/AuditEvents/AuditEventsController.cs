using AuditService.Features.AuditEvents.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Features.AuditEvents;

[ApiController, Route("api/audit")]
public sealed class AuditEventsController(AuditReportAuthorization authorization, AuditReportQuery reports, AuditCursorCodec cursors) : ControllerBase
{
    [HttpGet("events")]
    public async Task<IActionResult> Search([FromQuery] AuditEventSearchRequest request, CancellationToken cancellationToken)
    {
        var decision = await authorization.ResolveAsync(HttpContext);
        if (decision.Access is null) return Failure(decision.StatusCode);
        if (!cursors.IsConfigured) return Failure(503);
        var access = decision.Access;
        if ((request.IpAddress is not null || request.DeviceName is not null) && !access.SensitiveMetadata) return Failure(403);
        if (request.ApplicationId?.Length > 100 || request.TenantId?.Length > 128 || request.SourceService?.Length > 64
            || request.ActorUserId?.Length > 450 || request.Action?.Length > 160 || request.EntityType?.Length > 128
            || request.EntityId?.Length > 450 || request.DeviceName?.Length > 128 || request.IpAddress?.Length > 45
            || request.CorrelationId?.Length > 128) return Failure(400);
        if (!access.Scopes.Any(scope => (request.ApplicationId is null || request.ApplicationId == scope.ApplicationId)
            && (request.TenantId is null || request.TenantId == scope.TenantId)
            && (request.SourceService is null || request.SourceService == scope.SourceService))) return Failure(403);
        var page = await reports.SearchAsync(access, request, cancellationToken);
        return page is null ? Failure(400) : Success(page);
    }

    [HttpGet("events/{sourceService}/{eventId:guid}")]
    public async Task<IActionResult> Detail(string sourceService, Guid eventId, CancellationToken cancellationToken)
    {
        var decision = await authorization.ResolveAsync(HttpContext);
        if (decision.Access is null) return Failure(decision.StatusCode);
        if (!AuditService.Messaging.AuditMessagingConfiguration.ValidSource(sourceService)) return Failure(404);
        var entry = await reports.Scope(decision.Access).SingleOrDefaultAsync(x => x.SourceService == sourceService && x.EventId == eventId, cancellationToken);
        if (entry is null) return Failure(404);
        return Success(new
        {
            entry.EventId, entry.SourceService, entry.ApplicationId, entry.TenantId, entry.ActorKind,
            entry.ActorUserId, entry.ActorUserNameSnapshot, entry.ActorSystemId, entry.Action, entry.EntityType, entry.EntityId,
            entry.TimestampUtc, entry.IngestedAtUtc, entry.CorrelationId, entry.Status,
            IpAddress = decision.Access.SensitiveMetadata ? entry.IpAddress : null,
            DeviceName = decision.Access.SensitiveMetadata ? entry.DeviceName : null,
            UserAgent = decision.Access.SensitiveMetadata ? entry.UserAgent : null,
            OldValues = entry.OldValues is null ? (System.Text.Json.JsonElement?)null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(entry.OldValues),
            NewValues = entry.NewValues is null ? (System.Text.Json.JsonElement?)null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(entry.NewValues)
        });
    }

    [HttpGet("sources")]
    public async Task<IActionResult> Sources(CancellationToken cancellationToken)
    {
        var decision = await authorization.ResolveAsync(HttpContext);
        if (decision.Access is null) return Failure(decision.StatusCode);
        return Success(await reports.Scope(decision.Access).Select(x => new { x.ApplicationId, x.TenantId, x.SourceService }).Distinct().Take(100).ToArrayAsync(cancellationToken));
    }

    private ObjectResult Success(object data) => StatusCode(200, new { Success = true, StatusCode = 200, Data = data });
    private ObjectResult Failure(int status) => StatusCode(status, new { Success = false, StatusCode = status, Message = status == 503 ? "Audit reporting is temporarily unavailable." : "The request could not be authorized or completed." });
}
