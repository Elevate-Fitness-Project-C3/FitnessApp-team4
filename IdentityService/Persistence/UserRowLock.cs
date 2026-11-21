namespace IdentityService.Persistence;

internal static class UserRowLock
{
    public static async Task AcquireAsync(AuthDbContext db, string userId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT [Id] FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId};
            """, cancellationToken);
    }
}
