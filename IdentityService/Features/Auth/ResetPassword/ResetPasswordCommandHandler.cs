using IdentityService.Features.Users;
using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ResetPassword;

internal sealed class ResetPasswordCommandHandler(UserManager<ApplicationUser> users, AuthDbContext db, UserLockScope userLock,
    EmailVerificationCodeStore codes, ILogger<ResetPasswordCommandHandler> logger) : IRequestHandler<ResetPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
            return AuthErrors.InvalidCode;

        await using var transaction = await userLock.BeginAsync(user, cancellationToken);
        if (transaction is null || !user.EmailConfirmed)
            return AuthErrors.InvalidCode;

        var token = await codes.VerifyAsync(user, request.Code, OtpPurpose.ResetPassword, cancellationToken);
        if (token is null)
        {
            await transaction.CommitAsync(cancellationToken); // persists the failed-attempt counter
            return AuthErrors.InvalidCode;
        }

        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            return IdentityErrorMapper.ToResult<Success>(result); // no commit: the OTP is restored (e.g. weak password)

        var now = DateTime.UtcNow;
        await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedOn == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedOn, (DateTime?)now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Password reset succeeded. UserId={UserId}", user.Id);
        return Result.Success;
    }
}
