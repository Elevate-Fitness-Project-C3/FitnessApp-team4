using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.Login;

internal sealed class LoginCommandHandler(UserManager<ApplicationUser> users,
    IPasswordHasher<ApplicationUser> hasher, RefreshTokenIssuer issuer, AuthDbContext db, ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private static readonly ApplicationUser DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<ApplicationUser>()
        .HashPassword(DummyUser, Guid.NewGuid().ToString("N"));
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            hasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password);
            logger.LogWarning("Login rejected. UserId={UserId}", user?.Id);
            return AuthErrors.InvalidCredentials;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RefreshTokenSessionLock.AcquireAsync(db, user.Id, cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        if (db.Entry(user).State == EntityState.Detached) return AuthErrors.InvalidCredentials;
        var lockoutEnabled = users.SupportsUserLockout;
        if (lockoutEnabled && await users.IsLockedOutAsync(user))
        {
            hasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password);
            logger.LogWarning("Login rejected. UserId={UserId}", user?.Id);
            return AuthErrors.InvalidCredentials;
        }
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            if (lockoutEnabled)
            {
                var failedResult = await users.AccessFailedAsync(user);
                if (!failedResult.Succeeded)
                    logger.LogWarning("Login failure counter update failed. UserId={UserId}", user.Id);
            }
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Login rejected. UserId={UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }
        if (lockoutEnabled)
        {
            if (await users.IsLockedOutAsync(user))
                return AuthErrors.InvalidCredentials;
            var resetResult = await users.ResetAccessFailedCountAsync(user);
            if (!resetResult.Succeeded)
                return AuthErrors.InvalidCredentials;
        }
        if (!user.EmailConfirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthErrors.EmailNotConfirmed;
        }
        var tokens = await issuer.IssueAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Login succeeded. UserId={UserId}", user.Id);
        return AuthResponse.From(user, tokens);
    }
}
