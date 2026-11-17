namespace IdentityService.Email;

public sealed class EmailOutboxLease : IAsyncDisposable
{
    private readonly IServiceScopeFactory scopes;
    private readonly EmailOutboxOptions settings;
    private readonly ILogger logger;
    private readonly long messageId;
    private readonly Guid owner;
    private readonly CancellationTokenSource lifetime;
    private readonly Task renewal;
    public CancellationToken Token => lifetime.Token;

    public EmailOutboxLease(IServiceScopeFactory scopes, EmailOutboxOptions settings, ILogger logger,
        long messageId, Guid owner, CancellationToken cancellationToken)
    {
        this.scopes = scopes;
        this.settings = settings;
        this.logger = logger;
        this.messageId = messageId;
        this.owner = owner;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lifetime.CancelAfter(TimeSpan.FromSeconds(settings.ProcessingTimeoutSeconds));
        renewal = RenewLoopAsync();
    }

    public async Task EnsureOwnedAsync()
    {
        Token.ThrowIfCancellationRequested();
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var changed = await db.EmailOutbox
            .Where(x => x.Id == messageId && x.LockToken == owner && x.ProcessedOn == null && x.LockedUntil > DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil,
                x => (DateTime?)DateTime.UtcNow.AddSeconds(settings.LeaseSeconds)), Token);
        if (changed == 1) return;
        lifetime.Cancel();
        throw new OperationCanceledException("Outbox lease ownership was lost.", Token);
    }

    private async Task RenewLoopAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.RenewalSeconds));
            while (await timer.WaitForNextTickAsync(Token))
                await EnsureOwnedAsync();
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Outbox lease renewal failed. MessageId={MessageId}", messageId);
            lifetime.Cancel();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lifetime.Cancel();
        await renewal;
        lifetime.Dispose();
    }
}
