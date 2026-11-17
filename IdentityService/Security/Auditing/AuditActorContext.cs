using System.Security.Claims;

namespace IdentityService.Security.Auditing;

public sealed class AuditActorContext(IHttpContextAccessor accessor)
{
    public bool IsHttpRequest => accessor.HttpContext is not null;
    public string? UserId => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? accessor.HttpContext.User.FindFirstValue("sub") : null;
    public string? EntityActorId => accessor.HttpContext is null ? "system:identity" : UserId;
    public string CorrelationId => System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? accessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent
    {
        get
        {
            var value = accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(c => !char.IsControl(c)).Take(1024).ToArray());
        }
    }
}
