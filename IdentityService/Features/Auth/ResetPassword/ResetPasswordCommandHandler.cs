using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ResetPassword;
internal sealed class ResetPasswordCommandHandler(UserManager<ApplicationUser> users, AuthDbContext db, EmailVerificationCodeStore codes, ILogger<ResetPasswordCommandHandler> logger) : IRequestHandler<ResetPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed) return AuthErrors.InvalidCode;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RefreshTokenSessionLock.AcquireAsync(db, user.Id, cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        if (db.Entry(user).State == EntityState.Detached || !user.EmailConfirmed)
            return AuthErrors.InvalidCode;
        var token = await codes.VerifyAsync(user, request.Code, OtpPurpose.ResetPassword, cancellationToken);
        if (token is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return AuthErrors.InvalidCode;
        }
        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded) return Result<Success>.Failure(result.Errors.Select(x => Error.Validation(x.Code, x.Description)));
        var now = DateTime.UtcNow;
        await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedOn == null).ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password reset succeeded. UserId={UserId}", user.Id);
        return Result.Success;
    }
}
