using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler(IJwtProvider jwt, UserManager<ApplicationUser> users, AuthDbContext db,
    UserLockScope userLock, RefreshTokenIssuer issuer, ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = await jwt.ValidateTokenAsync(request.Token, cancellationToken);
        if (userId is null)
            return AuthErrors.InvalidAccessToken;

        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return AuthErrors.InvalidRefreshToken;

        var hash = RefreshTokenIssuer.Hash(request.RefreshToken);
        await using var transaction = await userLock.BeginAsync(user, cancellationToken);
        if (transaction is null || !user.IsActive || !user.EmailConfirmed || await users.IsLockedOutAsync(user))
            return AuthErrors.InvalidRefreshToken;

        var now = DateTime.UtcNow;
        var changed = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn == null && x.ExpiresOn > now)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        if (changed != 1)
        {
            var reused = await db.RefreshTokens.AnyAsync(
                x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn != null && x.ExpiresOn > now, cancellationToken);
            if (reused)
            {
                // A rotated/revoked token was replayed: treat it as theft and end every session of this user.
                await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedOn == null)
                    .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning("Refresh token reuse detected. UserId={UserId}", userId);
            }
            else
            {
                logger.LogWarning("Refresh token rejected. UserId={UserId}", userId);
            }
            return AuthErrors.InvalidRefreshToken;
        }

        var sessionId = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.TokenHash == hash)
            .Select(x => x.SessionId).SingleAsync(cancellationToken);
        var tokens = await issuer.IssueAsync(user, cancellationToken, sessionId);
        if (tokens is null)
            return AuthErrors.InvalidRefreshToken; // no commit: the old token stays valid

        await transaction.CommitAsync(cancellationToken);
        return AuthResponse.From(user, tokens);
    }
}
