using System.Text.Json;
using IdentityService.Features.Roles;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Users;

public sealed record SetAuditGrantCommand(string UserId, string ApplicationId, string? TenantId, string SourceService, bool Remove) : IRequest<Result<Success>>;
public sealed class SetAuditGrantValidator : AbstractValidator<SetAuditGrantCommand>
{
    public SetAuditGrantValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.ApplicationId).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(100).Must(x => x.Trim() == x && !x.Any(char.IsControl));
        RuleFor(x => x.SourceService).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(64).Must(x => x.Trim() == x && !x.Any(char.IsControl));
        RuleFor(x => x.TenantId).MaximumLength(128).Must(x => x is null || x.Length > 0 && x.Trim() == x && !x.Any(char.IsControl));
    }
}
public sealed class SetAuditGrantHandler(AuthDbContext db, AuditActorContext actor, SecurityMutationChecks checks, SecurityAuditWriter audit)
    : IRequestHandler<SetAuditGrantCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(SetAuditGrantCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync("Identity.AuditGrants.Manage", cancellationToken);
        if (error is not null) return error.Value;
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
        await transaction.CommitAsync(cancellationToken);
        return Result.Success;
    }
}
