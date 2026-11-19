using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(UserManager<ApplicationUser> users, UserLockScope userLock,
    EmailVerificationCodeStore codes, ILogger<ConfirmEmailCommandHandler> logger) : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return AuthErrors.InvalidCode;

        await using var transaction = await userLock.BeginAsync(user, cancellationToken);
        if (transaction is null || user.EmailConfirmed)
            return AuthErrors.InvalidCode;

        var token = await codes.VerifyAsync(user, request.Code, OtpPurpose.ConfirmEmail, cancellationToken);
        if (token is null)
        {
            await transaction.CommitAsync(cancellationToken); // persists the failed-attempt counter
            return AuthErrors.InvalidCode;
        }

        var result = await users.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return AuthErrors.InvalidCode; // no commit: the OTP is restored so the user can retry

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Email confirmed. UserId={UserId}", user.Id);
        return Result.Success;
    }
}
