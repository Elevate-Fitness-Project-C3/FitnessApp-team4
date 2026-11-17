using IdentityService.Email;
using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ResendConfirmationEmail;

internal sealed class ResendConfirmationEmailCommandHandler(UserManager<ApplicationUser> users, AuthEmailQueue emailQueue, AuthEmailSignal emailSignal)
    : IRequestHandler<ResendConfirmationEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            await emailQueue.EnqueueAsync(user.Id, OtpPurpose.ConfirmEmail, cancellationToken);
            emailSignal.Notify();
        }
        return Result.Success; // identical response whether or not the account exists
    }
}
