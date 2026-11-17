using IdentityService.Security.Tokens;
using Microsoft.EntityFrameworkCore.Storage;
namespace IdentityService.Persistence;

public sealed class UserLockScope(AuthDbContext db)
{
    public async Task<IDbContextTransaction?> BeginAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await RefreshTokenSessionLock.AcquireAsync(db, user.Id, cancellationToken);
            await db.Entry(user).ReloadAsync(cancellationToken);
            if (db.Entry(user).State != EntityState.Detached)
                return transaction; // Ownership passes to the caller only after setup succeeds.
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }

        await transaction.DisposeAsync();
        return null;
    }
}
