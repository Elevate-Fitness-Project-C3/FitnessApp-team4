using IdentityService.Common.Responses;
using IdentityService.Common;
using IdentityService.Features.Auth.ConfirmEmail;
using IdentityService.Features.Auth.ForgotPassword;
using IdentityService.Features.Auth.Login;
using IdentityService.Features.Auth.Logout;
using IdentityService.Features.Auth.RefreshToken;
using IdentityService.Features.Auth.Register;
using IdentityService.Features.Auth.ResendConfirmationEmail;
using IdentityService.Features.Auth.ResetPassword;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace IdentityService.Controllers;

[ApiController]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[Route("api/auth")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<RegisterUserResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new RegisterUserCommand(request.FirstName, request.LastName, request.Email, request.Password), cancellationToken))
            .ToActionResult(StatusCodes.Status201Created);

    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken)).ToActionResult();

    [HttpPost("confirm-email")]
    [ProducesResponseType<ApiResponse<Success>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ConfirmEmailCommand(request.Email, request.Code), cancellationToken)).ToActionResult();

    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType<ApiResponse<Success>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ResendConfirmationEmailCommand(request.Email), cancellationToken)).ToActionResult();

    [HttpPost("forgot-password")]
    [ProducesResponseType<ApiResponse<Success>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken)).ToActionResult();

    [HttpPost("reset-password")]
    [ProducesResponseType<ApiResponse<Success>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new ResetPasswordCommand(request.Email, request.Code, request.NewPassword), cancellationToken)).ToActionResult();

    [HttpPost("refresh")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new RefreshTokenCommand(request.Token, request.RefreshToken), cancellationToken)).ToActionResult();

    [HttpPost("logout")]
    [ProducesResponseType<ApiResponse<Success>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new LogoutCommand(request.Token, request.RefreshToken), cancellationToken)).ToActionResult();
}
