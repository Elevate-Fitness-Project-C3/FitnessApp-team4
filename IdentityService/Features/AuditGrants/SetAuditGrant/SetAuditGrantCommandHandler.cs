using IdentityService.Security.Authorization;
using System.Text.Json;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.AuditGrants.SetAuditGrant;

public sealed class SetAuditGrantCommandHandler(AuthDbContext db, AuditActorContext actor, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<SetAuditGrantCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(SetAuditGrantCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.AuditGrants.Manage", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<Success>> ApplyAsync(SetAuditGrantCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Set<AuditReportGrant>().AnyAsync(x => x.UserId == actor.UserId && x.ApplicationId == request.ApplicationId && x.TenantId == request.TenantId && x.SourceService == request.SourceService, cancellationToken)) return Error.Forbidden();
        if (!await db.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken)) return SecurityMutationChecks.Missing;
        var grant = await db.Set<AuditReportGrant>().SingleOrDefaultAsync(x => x.UserId == request.UserId && x.ApplicationId == request.ApplicationId && x.TenantId == request.TenantId && x.SourceService == request.SourceService, cancellationToken);
        if (request.Remove && grant is null || !request.Remove && grant is not null) return Result.Success;
        if (request.Remove)
        {
            // A delegated administrator cannot remove the scope from which their own authority derives.
            if (request.UserId == actor.UserId) return Error.Forbidden();
            db.Remove(grant!);
        }
        else
        {
            grant = new AuditReportGrant { Id = Guid.NewGuid(), UserId = request.UserId, ApplicationId = request.ApplicationId, TenantId = request.TenantId, SourceService = request.SourceService };
            db.Add(grant);
        }
        var values = new Dictionary<string, JsonElement>
        {
            ["applicationId"] = JsonSerializer.SerializeToElement(request.ApplicationId),
            ["tenantId"] = JsonSerializer.SerializeToElement(request.TenantId),
            ["sourceService"] = JsonSerializer.SerializeToElement(request.SourceService)
        };
        await audit.RecordAsync(request.Remove ? "Identity.AuditGrantRemoved" : "Identity.AuditGrantCreated", "AuditReportGrant", grant!.Id.ToString("D"), request.Remove ? values : null, request.Remove ? null : values, cancellationToken);
        return Result.Success;
    }
}
