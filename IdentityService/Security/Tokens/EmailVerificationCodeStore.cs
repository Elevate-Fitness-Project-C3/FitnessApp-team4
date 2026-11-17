using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using IdentityService.Security.Options;
namespace IdentityService.Security.Tokens;

public enum OtpIssueStatus { Issued, Deferred, Failed }
public sealed record OtpIssueResult(OtpIssueStatus Status, string? Code = null, DateTimeOffset? RetryAt = null);

public sealed class EmailVerificationCodeStore(UserManager<ApplicationUser> users, AuthDbContext db, IDataProtectionProvider protection,
    IOptions<OtpOptions> options, ILogger<EmailVerificationCodeStore> logger)
{
    // Legacy persisted identifier. Changing it requires a data migration.
    private const string Provider = "FitnessApp.EmailVerification";
    private const int MaxConcurrencyRetries = 3;
    private readonly IDataProtector protector = protection.CreateProtector("FitnessApp.EmailVerification.v1");
    private TimeSpan OtpLifetime => TimeSpan.FromMinutes(options.Value.ExpirationMinutes);

    private sealed record Challenge(string Code, string Token, DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt, DateTimeOffset AttemptWindowExpiresAt, int Attempts);

    private static string Purpose(OtpPurpose purpose) => purpose == OtpPurpose.ResetPassword ? "ResetPassword" : "ConfirmEmail";

    public async Task<OtpIssueResult> IssueAsync(ApplicationUser user, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("OTP issuance must run inside the per-user transaction (UserLockScope).");

        var settings = options.Value;
        var name = Purpose(purpose);
        var tokens = GetTokens(user.Id, name);
        for (var retry = 0; retry < MaxConcurrencyRetries; retry++)
        {
            var now = DateTimeOffset.UtcNow;
            var value = await tokens.Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            var existing = value is null ? null : ReadChallenge(value);
            if (value is not null && existing is null)
                logger.LogWarning("Replacing an unreadable OTP challenge. UserId={UserId} Purpose={Purpose}", user.Id, purpose);

            if (existing is not null)
            {
                var cooldownEndsAt = existing.IssuedAt.AddSeconds(settings.ResendCooldownSeconds);
                if (now < cooldownEndsAt)
                    return new OtpIssueResult(OtpIssueStatus.Deferred, RetryAt: cooldownEndsAt);
            }
            var activeWindow = existing is not null && existing.AttemptWindowExpiresAt > now;
            var attempts = activeWindow ? existing!.Attempts : 0;
            if (attempts >= settings.MaxAttempts)
                return new OtpIssueResult(OtpIssueStatus.Deferred, RetryAt: existing!.AttemptWindowExpiresAt);

            var windowExpiresAt = activeWindow ? existing!.AttemptWindowExpiresAt : now.Add(OtpLifetime);
            var expiresAt = now.Add(OtpLifetime);
            if (expiresAt > windowExpiresAt)
                expiresAt = windowExpiresAt;

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            var token = purpose == OtpPurpose.ResetPassword
                ? await users.GeneratePasswordResetTokenAsync(user)
                : await users.GenerateEmailConfirmationTokenAsync(user);
            var challenge = new Challenge(code, token, now, expiresAt, windowExpiresAt, attempts);
            var replacement = protector.Protect(JsonSerializer.Serialize(challenge));

            if (value is null)
            {
                db.Set<IdentityUserToken<string>>().Add(new IdentityUserToken<string>
                {
                    UserId = user.Id,
                    LoginProvider = Provider,
                    Name = name,
                    Value = replacement
                });
                await db.SaveChangesAsync(cancellationToken);
                return new OtpIssueResult(OtpIssueStatus.Issued, Code: code);
            }

            var updated = await tokens.Where(x => x.Value == value)
                .ExecuteUpdateAsync(x => x.SetProperty(t => t.Value, replacement), cancellationToken);
            if (updated == 1)
                return new OtpIssueResult(OtpIssueStatus.Issued, Code: code);
        }
        logger.LogWarning("OTP issuance failed after concurrency retries. UserId={UserId} Purpose={Purpose}", user.Id, purpose);
        return new OtpIssueResult(OtpIssueStatus.Failed);
    }

    public async Task<string?> VerifyAsync(ApplicationUser user, string code, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var tokens = GetTokens(user.Id, Purpose(purpose));
        for (var retry = 0; retry < MaxConcurrencyRetries; retry++)
        {
            var value = await tokens.Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
            if (value is null)
                return null;
            var challenge = ReadChallenge(value);
            var now = DateTimeOffset.UtcNow;
            if (challenge is null || challenge.ExpiresAt <= now
                || challenge.AttemptWindowExpiresAt <= now || challenge.Attempts >= options.Value.MaxAttempts)
                return null;
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(challenge.Code), Encoding.UTF8.GetBytes(code));
            var row = tokens.Where(x => x.Value == value);
            if (matches)
                return await row.ExecuteDeleteAsync(cancellationToken) == 1 ? challenge.Token : null;
            logger.LogWarning("OTP verification failed. UserId={UserId} Purpose={Purpose}", user.Id, purpose);
            var replacement = protector.Protect(JsonSerializer.Serialize(challenge with { Attempts = challenge.Attempts + 1 }));
            if (await row.ExecuteUpdateAsync(x => x.SetProperty(t => t.Value, replacement), cancellationToken) == 1)
                return null;
        }
        return null;
    }

    private IQueryable<IdentityUserToken<string>> GetTokens(string userId, string name) =>
        db.Set<IdentityUserToken<string>>().Where(x => x.UserId == userId && x.LoginProvider == Provider && x.Name == name);

    private Challenge? ReadChallenge(string value)
    {
        try
        {
            var challenge = JsonSerializer.Deserialize<Challenge>(protector.Unprotect(value));
            if (challenge is null)
                return null;
            return challenge with
            {
                IssuedAt = challenge.IssuedAt == default ? challenge.ExpiresAt.Subtract(OtpLifetime) : challenge.IssuedAt,
                AttemptWindowExpiresAt = challenge.AttemptWindowExpiresAt == default ? challenge.ExpiresAt : challenge.AttemptWindowExpiresAt
            };
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
    }
}
