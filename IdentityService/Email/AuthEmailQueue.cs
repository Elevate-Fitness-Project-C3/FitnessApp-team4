using IdentityService.Security.Tokens;

namespace IdentityService.Email;

public sealed class AuthEmailQueue(AuthDbContext db, IOptions<EmailOutboxOptions> options)
{
    public async Task EnqueueAsync(string userId, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var maxDeliveryAttempts = options.Value.MaxDeliveryAttempts;
        var now = DateTime.UtcNow;

        var reactivated = await db.EmailOutbox
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.ProcessedOn == null &&
                        x.Attempts >= maxDeliveryAttempts && (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Attempts, 0)
                    .SetProperty(x => x.NextAttemptOn, now)
                    .SetProperty(x => x.LockedUntil, (DateTime?)null)
                    .SetProperty(x => x.LockToken, (Guid?)null), cancellationToken);

        if (reactivated > 0) return;

        var alreadyPending = await db.EmailOutbox.AnyAsync(
            x => x.UserId == userId && x.Purpose == purpose && x.ProcessedOn == null, cancellationToken);

        if (alreadyPending) return;

        var message = new EmailOutboxMessage
        {
            UserId = userId,
            Purpose = purpose,
            CreatedOn = now,
            NextAttemptOn = now
        };

        db.EmailOutbox.Add(message);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(DbIndexes.EmailOutboxPending))
        {
            db.Entry(message).State = EntityState.Detached;
        }
    }
}
