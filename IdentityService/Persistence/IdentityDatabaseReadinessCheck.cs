using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityService.Persistence;

public sealed class IdentityDatabaseReadinessCheck(AuthDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Identity database is unavailable.");
}
