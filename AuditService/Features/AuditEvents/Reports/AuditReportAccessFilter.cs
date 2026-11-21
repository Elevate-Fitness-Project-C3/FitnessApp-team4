using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuditService.Features.AuditEvents.Reports;

public sealed class AuditReportAccessFilter(AuditReportAuthorization authorization) : IAsyncActionFilter
{
    public const string ItemKey = "audit.access";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var decision = await authorization.ResolveAsync(context.HttpContext);
        if (decision.Access is null)
        {
            context.Result = new ObjectResult(new
            {
                Success = false,
                decision.StatusCode,
                Message = decision.StatusCode == 503 ? "Audit reporting is temporarily unavailable." : "The request could not be authorized or completed."
            })
            { StatusCode = decision.StatusCode };
            return;
        }
        context.HttpContext.Items[ItemKey] = decision.Access;
        await next();
    }
}
