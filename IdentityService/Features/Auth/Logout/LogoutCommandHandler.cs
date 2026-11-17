using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.Logout;
internal sealed class LogoutCommandHandler(IJwtProvider jwt, AuthDbContext db) : IRequestHandler<LogoutCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = jwt.ValidateToken(request.Token);
        if (userId is null) return AuthErrors.InvalidAccessToken;
        var hash = RefreshTokenIssuer.Hash(request.RefreshToken);
        var now = DateTime.UtcNow;
        var changed = await db.RefreshTokens.Where(x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn == null && x.ExpiresOn > now)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        return changed == 1 ? Result<Success>.Success(Result.Success) : Result<Success>.Failure(AuthErrors.InvalidRefreshToken);
    }
}
