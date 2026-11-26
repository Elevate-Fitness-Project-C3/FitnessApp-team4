using System.Security.Claims;
using IdentityService.Common;
using IdentityService.Features.Users.UpdateOwnProfile;
using IdentityService.Features.Users.UpdateUserProfile;
using Microsoft.AspNetCore.RateLimiting;
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
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub");
        if (userId is null) return Result<UserDto>.Failure([Error.Unauthorized()]).ToActionResult();
        return (await sender.Send(new GetUserByIdQuery(userId), cancellationToken)).ToActionResult();
    }

    [HttpPut("me/profile"), EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> UpdateMe(UserProfileRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new UpdateOwnProfileCommand(request.FirstName, request.LastName,
            request.PhoneNumber, request.ConcurrencyToken), cancellationToken)).ToActionResult();

    [HttpPut("{userId}/profile"), HasPermission("Identity.Users.Profile.Update"), EnableRateLimiting(RateLimitPolicies.Admin)]
    public async Task<IActionResult> UpdateProfile(string userId, UserProfileRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new UpdateUserProfileCommand(userId, request.FirstName, request.LastName,
            request.PhoneNumber, request.ConcurrencyToken), cancellationToken)).ToActionResult();
}
