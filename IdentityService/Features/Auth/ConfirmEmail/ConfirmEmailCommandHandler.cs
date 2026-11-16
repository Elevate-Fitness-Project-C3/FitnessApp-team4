using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(UserManager<ApplicationUser> users, AuthDbContext db,
    EmailVerificationCodeStore codes, ILogger<ConfirmEmailCommandHandler> logger) : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return AuthErrors.InvalidCode;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RefreshTokenSessionLock.AcquireAsync(db, user.Id, cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        if (db.Entry(user).State == EntityState.Detached || user.EmailConfirmed)
            return AuthErrors.InvalidCode;
        var token = await codes.VerifyAsync(user, request.Code, OtpPurpose.ConfirmEmail, cancellationToken);
        if (token is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthErrors.InvalidCode;
        }
        var result = await users.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return AuthErrors.InvalidCode;
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Email confirmed. UserId={UserId}", user.Id);
        return Result.Success;
    }
}
