using System.Security.Cryptography;
using System.Text;
using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController, Route("internal/audit/authorization"), HasPermission("Audit.Reports.Read")]
public sealed class InternalAuditAuthorizationController(AuthDbContext db, IConfiguration configuration, AuditActorContext actor, EffectivePermissionResolver permissions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var configured = configuration["AuditReporting:ServiceKey"];
        if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32) return StatusCode(503);
        var supplied = Request.Headers["X-Audit-Service-Key"].ToString();
        if (supplied.Length > 256 || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(configured))) return Forbid();
        var scopes = await db.Set<AuditReportGrant>().AsNoTracking().Where(x => x.UserId == actor.UserId)
            .OrderBy(x => x.Id).Select(x => new { x.ApplicationId, x.TenantId, x.SourceService }).Take(101).ToArrayAsync(cancellationToken);
        if (scopes.Length > 100) return StatusCode(503);
        return Ok(new { UserId = actor.UserId, SensitiveMetadata = (await permissions.ResolveAsync(actor.UserId!, cancellationToken)).Contains("Audit.Reports.ReadSensitiveMetadata"), Scopes = scopes });
    }
}
