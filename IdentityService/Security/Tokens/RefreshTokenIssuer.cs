using IdentityService.Security.Options;
using System.Security.Cryptography;
using System.Text;
namespace IdentityService.Security.Tokens;

public sealed class RefreshTokenIssuer(AuthDbContext db, UserManager<ApplicationUser> users, IJwtProvider jwt, IOptions<JwtOptions> options)
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<IssuedTokens?> IssueAsync(ApplicationUser user, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Token issuance must run inside the per-user transaction (UserLockScope).");
        if (!user.EmailConfirmed || user.LockoutEnd > DateTimeOffset.UtcNow)
            return null;

        var roles = await users.GetRolesAsync(user);
        var (token, expiresIn) = jwt.GenerateToken(user, roles);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = DateTime.UtcNow;
        var expires = now.AddDays(options.Value.RefreshTokenExpirationDays);

        var excess = await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedOn == null && x.ExpiresOn > now)
            .OrderByDescending(x => x.Id).Skip(options.Value.MaxActiveRefreshTokens - 1).Select(x => x.Id).ToListAsync(cancellationToken);
        if (excess.Count > 0)
            await db.RefreshTokens.Where(x => excess.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, SessionId = sessionId ?? Guid.NewGuid(),
            TokenHash = Hash(refresh), ExpiresOn = expires
        });
        await db.SaveChangesAsync(cancellationToken);
        return new IssuedTokens(token, expiresIn, refresh, expires);
    }
}
