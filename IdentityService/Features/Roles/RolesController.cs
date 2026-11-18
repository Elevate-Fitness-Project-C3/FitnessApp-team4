using IdentityService.Common.Responses;
using IdentityService.Features.Roles.CreateRole;
using IdentityService.Features.Roles.DeleteRole;
using IdentityService.Features.Roles.GetRole;
using IdentityService.Features.Roles.ListRoles;
using IdentityService.Features.Roles.RenameRole;
using IdentityService.Features.Roles.UpdateRolePermissions;
using IdentityService.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Features.Roles;

[ApiController, Route("api/roles")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet, HasPermission("Identity.Roles.Read")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
        => (await sender.Send(new ListRolesQuery(search, page, pageSize), cancellationToken)).ToActionResult();
    [HttpGet("{roleId}"), HttpGet("{roleId}/permissions"), HasPermission("Identity.Roles.Read")]
    public async Task<IActionResult> Get(string roleId, CancellationToken cancellationToken) => (await sender.Send(new GetRoleQuery(roleId), cancellationToken)).ToActionResult();
    [HttpPost, HasPermission("Identity.Roles.Create")]
    public async Task<IActionResult> Create(CreateRoleCommand request, CancellationToken cancellationToken) => (await sender.Send(request, cancellationToken)).ToActionResult(201);
    [HttpPut("{roleId}/name"), HasPermission("Identity.Roles.Rename")]
    public async Task<IActionResult> Rename(string roleId, RenameRoleRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new RenameRoleCommand(roleId, request.Name, request.ConcurrencyToken), cancellationToken)).ToActionResult();
    [HttpPut("{roleId}/permissions"), HasPermission("Identity.Roles.Permissions.Update")]
    public async Task<IActionResult> Permissions(string roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new UpdateRolePermissionsCommand(roleId, request.PermissionCodes, request.ConcurrencyToken), cancellationToken)).ToActionResult();
    [HttpDelete("{roleId}"), HasPermission("Identity.Roles.Delete")]
    public async Task<IActionResult> Delete(string roleId, [FromHeader(Name = "If-Match")] string concurrencyToken, CancellationToken cancellationToken)
        => (await sender.Send(new DeleteRoleCommand(roleId, concurrencyToken), cancellationToken)).ToActionResult();
}
