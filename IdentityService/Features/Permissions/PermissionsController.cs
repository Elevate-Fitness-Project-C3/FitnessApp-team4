using IdentityService.Common.Responses;
using IdentityService.Features.Permissions.GetPermissionTree;
using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Features.Permissions;

[ApiController, Route("api/permissions"), HasPermission("Identity.Permissions.Read")]
public sealed class PermissionsController(ISender sender, EffectivePermissionResolver permissions, AuditActorContext actor) : ControllerBase
{
    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromQuery] string? roleId, CancellationToken cancellationToken)
    {
        if (roleId is not null && !(await permissions.ResolveAsync(actor.UserId!, cancellationToken)).Contains("Identity.Roles.Read")) return Forbid();
        return (await sender.Send(new GetPermissionTreeQuery(roleId), cancellationToken)).ToActionResult();
    }
    [HttpGet]
    public async Task<IActionResult> Definitions(CancellationToken cancellationToken)
        => (await sender.Send(new GetPermissionTreeQuery(null), cancellationToken)).ToActionResult();
}
