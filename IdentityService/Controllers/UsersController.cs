using IdentityService.Common.Responses;
using IdentityService.Features.Users.GetUserById;
using IdentityService.Features.Users.ListUsers;
using IdentityService.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission("Identity.Users.Read")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] ListUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.TopError;

            return BadRequest(ApiResponse<PaginatedResult<UserDto>>.Fail(error.Description));
        }

        return Ok(ApiResponse<PaginatedResult<UserDto>>.Ok(result.Value));
    }

    [HttpGet("{id}")]
    [HasPermission("Identity.Users.Read")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<UserDto>.Fail(result.TopError.Description, StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<UserDto>.Ok(result.Value));
    }
}