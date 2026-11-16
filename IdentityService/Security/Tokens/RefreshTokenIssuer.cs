using IdentityService.Security.Options;
using System.Security.Cryptography;
using System.Text;
namespace IdentityService.Security.Tokens;
public sealed class RefreshTokenIssuer(AuthDbContext db, UserManager<ApplicationUser> users, IJwtProvider jwt, IOptions<JwtOptions> options)
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public async Task<IssuedTokens> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        await RefreshTokenSessionLock.AcquireAsync(db, user.Id, cancellationToken);
        var currentUser = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id, cancellationToken);
        if (currentUser.SecurityStamp != user.SecurityStamp || !currentUser.EmailConfirmed || currentUser.LockoutEnd > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("User security state changed during token issuance.");
        var roles = await users.GetRolesAsync(user);
        var (token, expiresIn) = jwt.GenerateToken(user, roles);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationDays);
        var now = DateTime.UtcNow;
        var excess = await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedOn == null && x.ExpiresOn > now)
            .OrderByDescending(x => x.Id).Skip(options.Value.MaxActiveRefreshTokens - 1).Select(x => x.Id).ToListAsync(cancellationToken);
        if (excess.Count > 0)
            await db.RefreshTokens.Where(x => excess.Contains(x.Id)).ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(refresh), ExpiresOn = expires });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new IssuedTokens(token, expiresIn, refresh, expires);
    }
}
