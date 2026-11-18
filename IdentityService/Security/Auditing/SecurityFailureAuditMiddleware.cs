using Audit.Contracts.V1;

using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace IdentityService.Security.Auditing;

public sealed class SecurityFailureAuditMiddleware(RequestDelegate next, IServiceScopeFactory scopes, ILogger<SecurityFailureAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var administrative = context.Request.Path.StartsWithSegments("/api/roles") || context.Request.Path.StartsWithSegments("/api/users") || context.Request.Path.StartsWithSegments("/api/audit-grants");
        var failed = false;
        try { await next(context); }
        catch { failed = true; throw; }
        finally
        {
            if (administrative && (failed || context.Response.StatusCode >= 400)
                && !context.Items.ContainsKey(AuditActorContext.CommittedMutationKey))
            {
                var denied = !failed && context.Response.StatusCode is 401 or 403;
                try
                {
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await using var scope = scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                    await using var transaction = await db.Database.BeginTransactionAsync(deadline.Token);
                    await scope.ServiceProvider.GetRequiredService<SecurityAuditWriter>().RecordAsync(
                        denied ? "Identity.SecurityOperationDenied" : "Identity.SecurityOperationFailed", "SecurityOperation", null,
                        null, null, deadline.Token, denied ? AuditOperationStatus.Denied : AuditOperationStatus.Failed);
                    await transaction.CommitAsync(deadline.Token);
                }
                catch (Exception exception)
                {
                    // Database exception messages can include values from rejected rows.
                    var sql = exception as SqlException ?? exception.InnerException as SqlException;
                    logger.LogWarning(
                        "Security attempt could not be durably recorded. TraceId={TraceId} Outcome={Outcome} ExceptionType={ExceptionType} HResult={HResult} SqlError={SqlError}",
                        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier, denied ? "Denied" : "Failed",
                        exception.GetType().FullName, exception.HResult, sql?.Number);
                }
            }
        }
    }
}
