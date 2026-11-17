namespace IdentityService.Security.Tokens;

public sealed class RefreshTokenCleanupWorker(IServiceScopeFactory scopes, ILogger<RefreshTokenCleanupWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                var cutoff = DateTime.UtcNow - Retention;
                var tokens = await db.RefreshTokens.Where(x => x.ExpiresOn < cutoff).ExecuteDeleteAsync(stoppingToken);
                var outbox = await db.EmailOutbox.Where(x => x.ProcessedOn != null && x.ProcessedOn < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (tokens + outbox > 0)
                    logger.LogInformation("Cleanup removed {Tokens} refresh tokens and {Outbox} outbox rows.", tokens,
                        outbox);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Refresh token cleanup failed.");
            }
        } while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}