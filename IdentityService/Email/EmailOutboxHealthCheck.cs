using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityService.Email;

public sealed class EmailOutboxHealthCheck(AuthDbContext db, IOptions<EmailOutboxOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var exhausted = await db.EmailOutbox.AnyAsync(
            x => x.ProcessedOn == null && x.Attempts >= options.Value.MaxDeliveryAttempts, cancellationToken);
        return exhausted
            ? HealthCheckResult.Degraded("Email outbox requires operator attention.")
            : HealthCheckResult.Healthy();
    }
}
