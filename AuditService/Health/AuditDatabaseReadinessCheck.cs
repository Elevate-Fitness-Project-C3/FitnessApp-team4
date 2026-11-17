using AuditService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuditService.Health;

public sealed class AuditDatabaseReadinessCheck(AuditDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Audit database is unavailable.");
            if (db.Database.HasPendingModelChanges())
                return HealthCheckResult.Unhealthy("Audit database model requires a migration.");
            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return HealthCheckResult.Unhealthy("Audit database migrations must be applied by deployment.");
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            // Health responses and framework health logs must not contain SQL exceptions or payloads.
            return HealthCheckResult.Unhealthy("Audit database readiness could not be verified.");
        }
    }
}
