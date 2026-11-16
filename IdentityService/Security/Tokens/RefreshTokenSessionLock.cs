namespace IdentityService.Security.Tokens;
internal static class RefreshTokenSessionLock
{
    public static async Task AcquireAsync(AuthDbContext db, string userId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT [Id] FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId};
            """, cancellationToken);
    }
}
