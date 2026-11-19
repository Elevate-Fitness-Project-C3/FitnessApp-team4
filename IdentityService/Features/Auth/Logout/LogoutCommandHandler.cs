using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.Logout;
internal sealed class LogoutCommandHandler(IJwtProvider jwt, AuthDbContext db,
    UserManager<ApplicationUser> users, UserLockScope userLock) : IRequestHandler<LogoutCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = await jwt.ValidateTokenAsync(request.Token, cancellationToken);
        if (userId is null) return AuthErrors.InvalidAccessToken;
        var user = await users.FindByIdAsync(userId);
        if (user is null) return AuthErrors.InvalidRefreshToken;

        await using var transaction = await userLock.BeginAsync(user, cancellationToken);
        if (transaction is null) return AuthErrors.InvalidRefreshToken;

        var hash = RefreshTokenIssuer.Hash(request.RefreshToken);
        var now = DateTime.UtcNow;
        // A rotated token still identifies its session, so a concurrent refresh cannot escape logout.
        var sessionId = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.TokenHash == hash && x.ExpiresOn > now)
            .Select(x => (Guid?)x.SessionId).SingleOrDefaultAsync(cancellationToken);
        if (sessionId is null) return AuthErrors.InvalidRefreshToken;

        await db.RefreshTokens.Where(x => x.UserId == userId && x.SessionId == sessionId.Value && x.RevokedOn == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success;
    }
}
