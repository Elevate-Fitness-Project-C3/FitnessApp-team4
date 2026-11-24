using IdentityService.Common;
using IdentityService.Common.Responses;
using IdentityService.Features.AuditGrants.SetAuditGrant;
using IdentityService.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.AuditGrants;

[ApiController, EnableRateLimiting(RateLimitPolicies.Admin), Route("api/audit-grants"), HasPermission("Identity.AuditGrants.Manage")]
public sealed class AuditGrantsController(ISender sender) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> Grant(AuditGrantRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new SetAuditGrantCommand(request.UserId, request.ApplicationId, request.TenantId, request.SourceService, false), cancellationToken)).ToActionResult();
    [HttpDelete]
    public async Task<IActionResult> Remove(AuditGrantRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new SetAuditGrantCommand(request.UserId, request.ApplicationId, request.TenantId, request.SourceService, true), cancellationToken)).ToActionResult();
}
