using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.Login;

internal sealed class LoginCommandHandler(UserManager<ApplicationUser> users, IPasswordHasher<ApplicationUser> hasher,
    RefreshTokenIssuer issuer, UserLockScope userLock, ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private static readonly ApplicationUser DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<ApplicationUser>()
        .HashPassword(DummyUser, Guid.NewGuid().ToString("N"));

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            SpendHashingTime(request.Password);
            logger.LogWarning("Login rejected: unknown account.");
            return AuthErrors.InvalidCredentials;
        }

        if (await users.IsLockedOutAsync(user))
        {
            SpendHashingTime(request.Password);
            logger.LogWarning("Login rejected: account locked out. UserId={UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }

        var passwordIsValid = await users.CheckPasswordAsync(user, request.Password);
        // CheckPasswordAsync may upgrade an old hash. Compare the verified version after reloading.
        var verifiedPasswordHash = user.PasswordHash;
        await using var transaction = await userLock.BeginAsync(user, cancellationToken);
        if (transaction is null)
            return AuthErrors.InvalidCredentials;
        if (!string.Equals(verifiedPasswordHash, user.PasswordHash, StringComparison.Ordinal))
        {
            logger.LogWarning("Login rejected: password changed during authentication. UserId={UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }
        if (await users.IsLockedOutAsync(user))
            return AuthErrors.InvalidCredentials;

        if (!passwordIsValid)
        {
            var failed = await users.AccessFailedAsync(user);
            if (!failed.Succeeded)
            {
                logger.LogError("Login failure counter update failed. UserId={UserId}", user.Id);
                return AuthErrors.InvalidCredentials;
            }
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Login rejected: invalid password. UserId={UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }
        var reset = await users.ResetAccessFailedCountAsync(user);
        if (!reset.Succeeded)
        {
            logger.LogError("Could not reset the failed-access counter. UserId={UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }

        if (!user.EmailConfirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthErrors.EmailNotConfirmed; // reached only AFTER the password was verified
        }

        var tokens = await issuer.IssueAsync(user, cancellationToken);
        if (tokens is null)
            return AuthErrors.InvalidCredentials;

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Login succeeded. UserId={UserId}", user.Id);
        return AuthResponse.From(user, tokens);
    }

    private void SpendHashingTime(string password) => hasher.VerifyHashedPassword(DummyUser, DummyHash, password);
}
