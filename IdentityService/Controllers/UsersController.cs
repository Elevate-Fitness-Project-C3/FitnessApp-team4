using IdentityService.Features.Users.ListUsers;
using IdentityService.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public sealed class UsersController(ISender sender) : ControllerBase
    {
        [HttpGet]
        [HasPermission("Identity.Users.Read")]
        [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<UserDto>>> GetUsers([FromQuery] ListUsersQuery query,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}