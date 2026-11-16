using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.RefreshToken;
internal sealed class RefreshTokenCommandHandler(IJwtProvider jwt, UserManager<ApplicationUser> users, AuthDbContext db, RefreshTokenIssuer issuer, ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = jwt.ValidateToken(request.Token);
        if (userId is null) return AuthErrors.InvalidAccessToken;
        var user = await users.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed || await users.IsLockedOutAsync(user)) return AuthErrors.InvalidRefreshToken;
        var hash = RefreshTokenIssuer.Hash(request.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RefreshTokenSessionLock.AcquireAsync(db, userId, cancellationToken);
        var now = DateTime.UtcNow;
        var current = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (current is null || current.SecurityStamp != user.SecurityStamp || !current.EmailConfirmed || current.LockoutEnd > DateTimeOffset.UtcNow)
            return AuthErrors.InvalidRefreshToken;
        var changed = await db.RefreshTokens.Where(x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn == null && x.ExpiresOn > now)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        if (changed != 1)
        {
            var reused = await db.RefreshTokens.AnyAsync(x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn != null && x.ExpiresOn > now, cancellationToken);
            if (reused)
            {
                await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedOn == null)
                    .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning("Refresh token reuse detected. UserId={UserId}", userId);
            }
            else logger.LogWarning("Refresh token rejected. UserId={UserId}", userId);
            return AuthErrors.InvalidRefreshToken;
        }
        var response = await issuer.IssueAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthResponse.From(user, response);
    }
}
