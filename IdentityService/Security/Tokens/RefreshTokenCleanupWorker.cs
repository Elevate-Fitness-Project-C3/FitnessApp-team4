namespace IdentityService.Security.Tokens;
public sealed class RefreshTokenCleanupWorker(IServiceScopeFactory scopes, ILogger<RefreshTokenCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-7);
                await db.RefreshTokens.Where(x => x.ExpiresOn < cutoff).ExecuteDeleteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("Refresh cleanup failed. ErrorType={ErrorType}", ex.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
