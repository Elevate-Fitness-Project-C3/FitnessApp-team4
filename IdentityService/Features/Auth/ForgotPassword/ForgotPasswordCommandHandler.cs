using IdentityService.Email;
using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(UserManager<ApplicationUser> users, AuthEmailQueue emailQueue, AuthEmailSignal emailSignal)
    : IRequestHandler<ForgotPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && user.EmailConfirmed)
        {
            await emailQueue.EnqueueAsync(user.Id, OtpPurpose.ResetPassword, cancellationToken);
            emailSignal.Notify();
        }
        return Result.Success; // identical response whether or not the account exists
    }
}
