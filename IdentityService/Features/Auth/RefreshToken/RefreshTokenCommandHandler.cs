using IdentityService.Email;
using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.RefreshToken;
internal sealed class RefreshTokenCommandHandler(IJwtProvider jwt, UserManager<ApplicationUser> users, AuthDbContext db, RefreshTokenIssuer issuer) : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = jwt.ValidateToken(request.Token);
        if (userId is null) return AuthErrors.InvalidRefreshToken;
        var user = await users.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed || await users.IsLockedOutAsync(user)) return AuthErrors.InvalidRefreshToken;
        var hash = RefreshTokenIssuer.Hash(request.RefreshToken);
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var changed = await db.RefreshTokens.Where(x => x.UserId == userId && x.TokenHash == hash && x.RevokedOn == null && x.ExpiresOn > now)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        if (changed != 1) return AuthErrors.InvalidRefreshToken;
        var response = await issuer.IssueAsync(user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
