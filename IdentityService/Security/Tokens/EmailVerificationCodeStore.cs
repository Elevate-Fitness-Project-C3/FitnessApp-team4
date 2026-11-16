using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Globalization;
namespace IdentityService.Security.Tokens;

public sealed class EmailVerificationCodeStore(UserManager<ApplicationUser> users, AuthDbContext db, IDataProtectionProvider protection, IOptions<IdentityService.Security.Options.EmailConfirmationTokenOptions> options, ILogger<EmailVerificationCodeStore> logger)
{
    private const string Provider = "FitnessApp.EmailVerification";
    private const int MaxAttempts = 5;
    private TimeSpan OtpLifetime => TimeSpan.FromMinutes(options.Value.ExpirationMinutes);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private TimeSpan AttemptWindow => OtpLifetime;
    private readonly IDataProtector protector = protection.CreateProtector("FitnessApp.EmailVerification.v1");
    private sealed record Challenge(string Code, string Token, DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt, DateTimeOffset AttemptWindowExpiresAt, int Attempts);
    private static string Purpose(OtpPurpose purpose) => purpose == OtpPurpose.ResetPassword ? "ResetPassword" : "ConfirmEmail";
    public async Task<string?> IssueAsync(ApplicationUser user, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var name = Purpose(purpose);
        var tokens = GetTokens(user.Id, name);
        for (var retry = 0; retry < 3; retry++)
        {
            var now = DateTimeOffset.UtcNow;
            var value = await tokens.Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            var existing = value is null ? null : ReadChallenge(value);
            if (value is not null && existing is null)
                return null;
            if (existing is not null && now - existing.IssuedAt < ResendCooldown)
                return null;
            var activeWindow = existing is not null && existing.AttemptWindowExpiresAt > now;
            var attempts = activeWindow ? existing!.Attempts : 0;
            if (attempts >= MaxAttempts)
                return null;
            var windowExpiresAt = activeWindow ? existing!.AttemptWindowExpiresAt : now.Add(AttemptWindow);
            var expiresAt = now.Add(OtpLifetime);
            if (expiresAt > windowExpiresAt)
                expiresAt = windowExpiresAt;
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            var token = purpose == OtpPurpose.ResetPassword ? await users.GeneratePasswordResetTokenAsync(user) : await users.GenerateEmailConfirmationTokenAsync(user);
            var challenge = new Challenge(code, token, now, expiresAt, windowExpiresAt, attempts);
            var replacement = protector.Protect(JsonSerializer.Serialize(challenge));
            if (value is null)
            {
                var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO [UserTokens] ([UserId], [LoginProvider], [Name], [Value])
                    SELECT {user.Id}, {Provider}, {name}, {replacement}
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [UserTokens] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [UserId] = {user.Id}
                            AND [LoginProvider] = {Provider}
                            AND [Name] = {name}
                    );
                    """, cancellationToken);
                if (inserted == 1)
                    return code;
            }
            else
            {
                var updated = await tokens.Where(x => x.Value == value)
                    .ExecuteUpdateAsync(x => x.SetProperty(t => t.Value, replacement), cancellationToken);
                if (updated == 1)
                    return code;
            }
        }
        return null;
    }
    // Call within the same database transaction as the identity operation.
    public async Task<string?> VerifyAsync(ApplicationUser user, string code, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var tokens = GetTokens(user.Id, Purpose(purpose));
        for (var retry = 0; retry < 3; retry++)
        {
            var value = await tokens.Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            if (value is null)
                return null;
            var challenge = ReadChallenge(value);
            var now = DateTimeOffset.UtcNow;
            if (challenge is null || challenge.ExpiresAt <= now
                || challenge.AttemptWindowExpiresAt <= now || challenge.Attempts >= MaxAttempts)
                return null;
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(challenge.Code), Encoding.UTF8.GetBytes(code));
            var updated = tokens.Where(x => x.Value == value);
            if (matches)
                return await updated.ExecuteDeleteAsync(cancellationToken) == 1 ? challenge.Token : null;
            logger.LogWarning("OTP verification failed. UserId={UserId} Purpose={Purpose}", user.Id, purpose);
            var replacement = protector.Protect(JsonSerializer.Serialize(challenge with { Attempts = challenge.Attempts + 1 }));
            if (await updated.ExecuteUpdateAsync(x => x.SetProperty(t => t.Value, replacement), cancellationToken) == 1)
                return null;
        }
        return null;
    }
    private IQueryable<IdentityUserToken<string>> GetTokens(string userId, string name)
    {
        return db.Set<IdentityUserToken<string>>()
            .Where(x => x.UserId == userId && x.LoginProvider == Provider && x.Name == name);
    }
    private Challenge? ReadChallenge(string value)
    {
        try
        {
            var challenge = JsonSerializer.Deserialize<Challenge>(protector.Unprotect(value));
            if (challenge is null)
                return null;
            // Preserve compatibility with previously issued challenges.
            return challenge with
            {
                IssuedAt = challenge.IssuedAt == default
                    ? challenge.ExpiresAt.Subtract(OtpLifetime) : challenge.IssuedAt,
                AttemptWindowExpiresAt = challenge.AttemptWindowExpiresAt == default
                    ? challenge.ExpiresAt : challenge.AttemptWindowExpiresAt
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
    }
}
