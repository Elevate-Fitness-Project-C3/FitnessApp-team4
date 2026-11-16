using System.Security.Cryptography;
using System.Text.Json;
namespace IdentityService.Security.Tokens;
public sealed class EmailVerificationCodeStore(UserManager<ApplicationUser> users, AuthDbContext db, IDataProtectionProvider protection)
{
    private const string Provider = "FitnessApp.EmailVerification";
    private const int MaxAttempts = 5;
    private readonly IDataProtector protector = protection.CreateProtector("FitnessApp.EmailVerification.v1");
    private sealed record Challenge(string Code, string Token, DateTimeOffset ExpiresAt, int Attempts);
    private static string Purpose(bool reset) => reset ? "ResetPassword" : "ConfirmEmail";
    public async Task<string?> IssueAsync(ApplicationUser user, bool reset)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var token = reset ? await users.GeneratePasswordResetTokenAsync(user) : await users.GenerateEmailConfirmationTokenAsync(user);
        var challenge = new Challenge(code, token, DateTimeOffset.UtcNow.AddMinutes(10), 0);
        var result = await users.SetAuthenticationTokenAsync(user, Provider, Purpose(reset), protector.Protect(JsonSerializer.Serialize(challenge)));
        return result.Succeeded ? code : null;
    }
    // Call within the same database transaction as the identity operation.
    public async Task<string?> VerifyAsync(ApplicationUser user, string code, bool reset, CancellationToken cancellationToken)
    {
        var name = Purpose(reset);
        var tokens = db.Set<IdentityUserToken<string>>().Where(x => x.UserId == user.Id && x.LoginProvider == Provider && x.Name == name);
        while (true)
        {
            var value = await tokens.Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            if (value is null) return null;
            Challenge? challenge;
            try { challenge = JsonSerializer.Deserialize<Challenge>(protector.Unprotect(value)); }
            catch (Exception ex) when (ex is CryptographicException or JsonException) { return null; }
            if (challenge is null || challenge.ExpiresAt <= DateTimeOffset.UtcNow || challenge.Attempts >= MaxAttempts) return null;
            var matches = CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(challenge.Code), System.Text.Encoding.UTF8.GetBytes(code));
            var updated = tokens.Where(x => x.Value == value);
            if (matches)
            {
                return await updated.ExecuteDeleteAsync(cancellationToken) == 1 ? challenge.Token : null;
            }
            var replacement = protector.Protect(JsonSerializer.Serialize(challenge with { Attempts = challenge.Attempts + 1 }));
            if (await updated.ExecuteUpdateAsync(x => x.SetProperty(t => t.Value, replacement), cancellationToken) == 1) return null;
        }
    }
}
