using Microsoft.AspNetCore.RateLimiting;
using IdentityService.Common;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Features.Auth.Login;
using IdentityService.Features.Auth.ConfirmEmail;
using IdentityService.Features.Auth.ResendConfirmationEmail;
using IdentityService.Features.Auth.ForgetPassword;
using IdentityService.Features.Auth.ResetPassword;
using IdentityService.Features.Auth.RefreshToken;
using IdentityService.Features.Auth.Logout;
using IdentityService.Features.Auth.Register;
using IdentityService.Common.Responses;
namespace IdentityService.Controllers;

[ApiController]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterUserCommand(request.FirstName, request.LastName, request.Email, request.Password), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status201Created);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<Success>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmEmailCommand(request.Email, request.Code), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<Success>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [HttpPost("resend-confirmation-email")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResendConfirmationEmailCommand(request.Email), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<Success>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ForgetPasswordCommand(request.Email), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<Success>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPasswordCommand(request.Email, request.Code, request.NewPassword), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.Token, request.RefreshToken), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
    [ProducesResponseType(typeof(ApiResponse<Success>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.Token, request.RefreshToken), cancellationToken);
        var apiResponse = result.ToApiResponse(StatusCodes.Status200OK);
        return StatusCode(apiResponse.StatusCode, apiResponse);
    }
}
