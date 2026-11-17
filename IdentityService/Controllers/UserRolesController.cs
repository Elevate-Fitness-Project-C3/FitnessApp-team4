using System.Security.Claims;
using IdentityService.Features.Roles;
using IdentityService.Features.Users;
using IdentityService.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController, Route("api/users")]
public sealed class UserRolesController(ISender sender) : ControllerBase
{
    [HttpGet("{userId}/roles"), HasPermission("Identity.Users.Roles.Read")]
    public async Task<IActionResult> Roles(string userId, CancellationToken cancellationToken) => (await sender.Send(new GetUserRolesQuery(userId), cancellationToken)).ToActionResult();
    [HttpGet("{userId}/permissions"), HasPermission("Identity.Users.Permissions.Read")]
    public async Task<IActionResult> Permissions(string userId, CancellationToken cancellationToken) => (await sender.Send(new GetUserPermissionsQuery(userId), cancellationToken)).ToActionResult();
    [HttpGet("me/permissions"), Authorize]
    public async Task<IActionResult> OwnPermissions(CancellationToken cancellationToken) => (await sender.Send(new GetUserPermissionsQuery(User.FindFirstValue("sub")!), cancellationToken)).ToActionResult();
    [HttpPut("{userId}/roles/{roleId}"), HasPermission("Identity.Users.Roles.Assign")]
    public async Task<IActionResult> Assign(string userId, string roleId, UserRoleRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new SetUserRoleCommand(userId, roleId, true, request.ConcurrencyToken), cancellationToken)).ToActionResult();
    [HttpDelete("{userId}/roles/{roleId}"), HasPermission("Identity.Users.Roles.Remove")]
    public async Task<IActionResult> Remove(string userId, string roleId, [FromHeader(Name = "If-Match")] string concurrencyToken, CancellationToken cancellationToken)
        => (await sender.Send(new SetUserRoleCommand(userId, roleId, false, concurrencyToken), cancellationToken)).ToActionResult();
    [HttpPut("{userId}/status"), HasPermission("Identity.Users.Update")]
    public async Task<IActionResult> Status(string userId, UserStatusRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new SetUserStatusCommand(userId, request.IsActive, request.ConcurrencyToken), cancellationToken)).ToActionResult();
}
