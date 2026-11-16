using IdentityService.Features.Auth.Login;
using IdentityService.Security.Options;
using System.Security.Cryptography;
using System.Text;
namespace IdentityService.Security.Tokens;
public sealed class RefreshTokenIssuer(AuthDbContext db, UserManager<ApplicationUser> users, IJwtProvider jwt, IOptions<JwtOptions> options)
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await users.GetRolesAsync(user);
        var (token, expiresIn) = jwt.GenerateToken(user, roles);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationDays);
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(refresh), ExpiresOn = expires });
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(user.Id, user.Email!, user.FirstName, user.LastName, token, expiresIn, refresh, expires);
    }
}
