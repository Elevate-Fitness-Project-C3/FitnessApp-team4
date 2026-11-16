using IdentityService.Security.Tokens;
namespace IdentityService.Email;
public sealed class AuthEmailQueue(AuthDbContext db)
{
    internal const string Provider = "FitnessApp.AuthEmailQueue";
    public async Task EnqueueAsync(string userId, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var name = purpose.ToString();
        var value = Guid.NewGuid().ToString("N");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [UserTokens] ([UserId], [LoginProvider], [Name], [Value])
            SELECT {userId}, {Provider}, {name}, {value}
            WHERE NOT EXISTS (SELECT 1 FROM [UserTokens] WITH (UPDLOCK, HOLDLOCK)
                WHERE [UserId] = {userId} AND [LoginProvider] = {Provider} AND [Name] = {name});
            """, cancellationToken);
    }
}
