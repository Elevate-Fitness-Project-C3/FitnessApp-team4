using IdentityService.Common.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace IdentityService.Security.Authorization;

public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Succeeded) { await next(context); return; }
        if (result.Challenged) await context.ChallengeAsync();
        else await context.ForbidAsync();
        if (!context.Response.HasStarted)
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(result.Challenged ? "Authentication is required." : "The required permission is not granted.", result.Challenged ? 401 : 403), context.RequestAborted);
    }
}
