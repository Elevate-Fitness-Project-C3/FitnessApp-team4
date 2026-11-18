using System.Linq.Expressions;
using AuditService.Auditing;
using AuditService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Features.AuditEvents.Reports;

public sealed class AuditReportQuery(AuditDbContext db, AuditCursorCodec cursors)
{
    public IQueryable<AuditEvent> Scope(AuditAccess access)
    {
        var parameter = Expression.Parameter(typeof(AuditEvent), "entry");
        Expression predicate = Expression.Constant(false);
        foreach (var scope in access.Scopes)
        {
            Expression Match(string name, string? value) => Expression.Equal(Expression.Property(parameter, name), Expression.Constant(value, typeof(string)));
            predicate = Expression.OrElse(predicate, Expression.AndAlso(Match(nameof(AuditEvent.ApplicationId), scope.ApplicationId),
                Expression.AndAlso(Match(nameof(AuditEvent.TenantId), scope.TenantId), Match(nameof(AuditEvent.SourceService), scope.SourceService))));
        }
        return db.AuditEvents.AsNoTracking().Where(Expression.Lambda<Func<AuditEvent, bool>>(predicate, parameter));
    }

    public async Task<AuditEventPageDto?> SearchAsync(AuditAccess access, AuditEventSearchRequest request, CancellationToken cancellationToken)
    {
        if (request.FromUtc == default || request.FromUtc.Offset != TimeSpan.Zero || request.ToUtc.Offset != TimeSpan.Zero
            || request.ToUtc <= request.FromUtc || request.ToUtc - request.FromUtc > TimeSpan.FromDays(90)
            || request.PageSize is < 1 or > 200 || request.Sort is not ("timestampUtc" or "ingestedAtUtc")
            || request.Status is not null && !Enum.IsDefined(request.Status.Value)) return null;
        var query = Scope(access).Where(x => x.TimestampUtc >= request.FromUtc && x.TimestampUtc < request.ToUtc);
        if (request.ApplicationId is not null) query = query.Where(x => x.ApplicationId == request.ApplicationId);
        if (request.TenantId is not null) query = query.Where(x => x.TenantId == request.TenantId);
        if (request.SourceService is not null) query = query.Where(x => x.SourceService == request.SourceService);
        if (request.ActorUserId is not null) query = query.Where(x => x.ActorUserId == request.ActorUserId);
        if (request.Action is not null) query = query.Where(x => x.Action == request.Action);
        if (request.EntityType is not null) query = query.Where(x => x.EntityType == request.EntityType);
        if (request.EntityId is not null) query = query.Where(x => x.EntityId == request.EntityId);
        if (request.DeviceName is not null) query = query.Where(x => x.DeviceName == request.DeviceName);
        if (request.IpAddress is not null) query = query.Where(x => x.IpAddress == request.IpAddress);
        if (request.CorrelationId is not null) query = query.Where(x => x.CorrelationId == request.CorrelationId);
        if (request.Status is not null) query = query.Where(x => x.Status == request.Status);
        var hash = AuditCursorCodec.FilterHash(request, access);
        if (request.Cursor is not null)
        {
            var cursor = cursors.Decode(request.Cursor, hash);
            if (cursor is null) return null;
            if (request.Sort == "timestampUtc") query = request.Descending
                ? query.Where(x => x.TimestampUtc < cursor.Time || x.TimestampUtc == cursor.Time && x.Id < cursor.Id)
                : query.Where(x => x.TimestampUtc > cursor.Time || x.TimestampUtc == cursor.Time && x.Id > cursor.Id);
            else query = request.Descending
                ? query.Where(x => x.IngestedAtUtc < cursor.Time || x.IngestedAtUtc == cursor.Time && x.Id < cursor.Id)
                : query.Where(x => x.IngestedAtUtc > cursor.Time || x.IngestedAtUtc == cursor.Time && x.Id > cursor.Id);
        }
        query = request.Sort == "timestampUtc"
            ? request.Descending ? query.OrderByDescending(x => x.TimestampUtc).ThenByDescending(x => x.Id) : query.OrderBy(x => x.TimestampUtc).ThenBy(x => x.Id)
            : request.Descending ? query.OrderByDescending(x => x.IngestedAtUtc).ThenByDescending(x => x.Id) : query.OrderBy(x => x.IngestedAtUtc).ThenBy(x => x.Id);
        var rows = await query.Select(x => new
        {
            x.Id,
            Summary = new AuditEventSummaryDto(x.EventId, x.SourceService, x.ApplicationId, x.TenantId, x.ActorUserId,
                x.ActorUserNameSnapshot, x.Action, x.EntityType, x.EntityId, x.TimestampUtc, x.IngestedAtUtc, x.CorrelationId, x.Status)
        }).Take(request.PageSize + 1).ToListAsync(cancellationToken);
        var more = rows.Count > request.PageSize;
        var page = rows.Take(request.PageSize).ToArray();
        var last = page.LastOrDefault();
        var next = more && last is not null
            ? cursors.Encode(new(request.Sort == "timestampUtc" ? last.Summary.TimestampUtc : last.Summary.IngestedAtUtc, last.Id, hash))
            : null;
        return new(page.Select(x => x.Summary).ToArray(), next, more);
    }
}
