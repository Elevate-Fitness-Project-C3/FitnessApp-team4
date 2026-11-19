using IdentityService.Security.Options;
using IdentityService.Security.Tokens;

namespace IdentityService.Email;

public sealed class AuthEmailWorker(
    IServiceScopeFactory scopes,
    AuthEmailSignal signal,
    IOptions<OtpOptions> otp,
    IOptions<EmailOutboxOptions> outbox,
    ILogger<AuthEmailWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private int MaxDeliveryAttempts => outbox.Value.MaxDeliveryAttempts;
    private static readonly TimeSpan IdlePoll = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var handled = await ProcessBatchAsync(stoppingToken);
                if (handled == 0) await signal.WaitAsync(IdlePoll, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication e-mail worker failed.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var handled = 0;

        for (var i = 0; i < BatchSize; i++)
        {
            var now = DateTime.UtcNow;
            var messageId = await db.EmailOutbox.AsNoTracking()
                .Where(x => x.ProcessedOn == null && x.Attempts < MaxDeliveryAttempts && x.NextAttemptOn <= now &&
                            (x.LockedUntil == null || x.LockedUntil < now))
                .OrderBy(x => x.Id)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (messageId is null) break;

            var leaseToken = Guid.NewGuid();
            var leaseUntil = DateTime.UtcNow.AddSeconds(outbox.Value.LeaseSeconds);

            var claimed = await db.EmailOutbox
                .Where(x => x.Id == messageId.Value && x.ProcessedOn == null && x.Attempts < MaxDeliveryAttempts &&
                            x.NextAttemptOn <= now && (x.LockedUntil == null || x.LockedUntil < now))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.LockToken, (Guid?)leaseToken)
                        .SetProperty(x => x.LockedUntil, (DateTime?)leaseUntil), cancellationToken);

            if (claimed == 0) continue;

            var message = await db.EmailOutbox.AsNoTracking()
                .FirstAsync(x => x.Id == messageId.Value && x.LockToken == leaseToken, cancellationToken);

            await using var lease = new EmailOutboxLease(scopes, outbox.Value, logger, message.Id, leaseToken, cancellationToken);
            try
            {
                await ProcessMessageAsync(scope.ServiceProvider, db, message, leaseToken, lease, lease.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Outbox processing canceled after timeout or lease loss. MessageId={MessageId}", message.Id);
                await RetryLaterAsync(db, message, leaseToken, cancellationToken);
            }
            handled++;
        }

        return handled;
    }

    private async Task ProcessMessageAsync(IServiceProvider services, AuthDbContext db, EmailOutboxMessage message,
        Guid leaseToken, EmailOutboxLease lease, CancellationToken cancellationToken)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var codes = services.GetRequiredService<EmailVerificationCodeStore>();
        var userLock = services.GetRequiredService<UserLockScope>();
        var sender = services.GetRequiredService<AuthEmailSender>();

        var user = await users.FindByIdAsync(message.UserId);
        if (user is null)
        {
            await CompleteAsync(db, message.Id, leaseToken, cancellationToken);
            return;
        }

        OtpIssueResult? issue = null;
        var eligible = false;
        await using (var transaction = await userLock.BeginAsync(user, cancellationToken))
        {
            if (transaction is null)
            {
                await CompleteAsync(db, message.Id, leaseToken, cancellationToken);
                return;
            }

            await lease.EnsureOwnedAsync();
            eligible = IsEligible(user, message.Purpose);
            if (eligible) issue = await codes.IssueAsync(user, message.Purpose, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        if (!eligible)
        {
            await CompleteAsync(db, message.Id, leaseToken, cancellationToken);
            return;
        }

        if (issue is null || issue.Status == OtpIssueStatus.Failed)
        {
            await RetryLaterAsync(db, message, leaseToken, cancellationToken);
            return;
        }

        if (issue.Status == OtpIssueStatus.Deferred)
        {
            await DeferAsync(db, message.Id, leaseToken, issue.RetryAt!.Value, cancellationToken);
            return;
        }

        await lease.EnsureOwnedAsync();
        if (await sender.SendAsync(user, message.Purpose, issue.Code!, cancellationToken))
            await CompleteAsync(db, message.Id, leaseToken, cancellationToken);
        else
            await RetryLaterAsync(db, message, leaseToken, cancellationToken);
    }

    private static bool IsEligible(ApplicationUser user, OtpPurpose purpose) =>
        purpose == OtpPurpose.ResetPassword ? user.EmailConfirmed : !user.EmailConfirmed;

    private static Task CompleteAsync(AuthDbContext db, long id, Guid leaseToken, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return db.EmailOutbox.Where(x => x.Id == id && x.LockToken == leaseToken && x.LockedUntil > DateTime.UtcNow && x.ProcessedOn == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.ProcessedOn, (DateTime?)now)
                    .SetProperty(x => x.LockedUntil, (DateTime?)null)
                    .SetProperty(x => x.LockToken, (Guid?)null), cancellationToken);
    }

    private static Task DeferAsync(AuthDbContext db, long id, Guid leaseToken, DateTimeOffset retryAt,
        CancellationToken cancellationToken)
    {
        var nextAttempt = retryAt.UtcDateTime;
        return db.EmailOutbox.Where(x => x.Id == id && x.LockToken == leaseToken && x.LockedUntil > DateTime.UtcNow && x.ProcessedOn == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.NextAttemptOn, nextAttempt)
                    .SetProperty(x => x.LockedUntil, (DateTime?)null)
                    .SetProperty(x => x.LockToken, (Guid?)null), cancellationToken);
    }

    private async Task RetryLaterAsync(AuthDbContext db, EmailOutboxMessage message, Guid leaseToken,
        CancellationToken cancellationToken)
    {
        var attempts = message.Attempts + 1;
        // The delay must exceed the resend cooldown, otherwise the retry could not issue a fresh OTP.
        var nextAttempt = DateTime.UtcNow + TimeSpan.FromSeconds(otp.Value.ResendCooldownSeconds + 30) * attempts;
        var changed = await db.EmailOutbox.Where(x => x.Id == message.Id && x.LockToken == leaseToken && x.LockedUntil > DateTime.UtcNow && x.ProcessedOn == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Attempts, attempts)
                    .SetProperty(x => x.NextAttemptOn, nextAttempt)
                    .SetProperty(x => x.LockedUntil, (DateTime?)null)
                    .SetProperty(x => x.LockToken, (Guid?)null), cancellationToken);
        if (changed == 1 && attempts >= MaxDeliveryAttempts)
            logger.LogError("Authentication e-mail exhausted delivery attempts. MessageId={MessageId} Attempts={Attempts}", message.Id, attempts);
    }
}
