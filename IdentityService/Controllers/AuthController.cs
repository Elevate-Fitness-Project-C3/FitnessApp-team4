using Microsoft.AspNetCore.RateLimiting;
using IdentityService.Features.Auth.Login;
using IdentityService.Features.Auth.ConfirmEmail;
using IdentityService.Features.Auth.ResendConfirmationEmail;
using IdentityService.Features.Auth.ForgetPassword;
using IdentityService.Features.Auth.ResetPassword;
using IdentityService.Features.Auth.RefreshToken;
using IdentityService.Features.Auth.Logout;

using IdentityService.Common.Responses;
using IdentityService.Features.Auth.Register;
using Microsoft.AspNetCore.Mvc;
namespace IdentityService.Controllers;

[ApiController]
[EnableRateLimiting("Auth")]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status201Created);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("forget-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
}
