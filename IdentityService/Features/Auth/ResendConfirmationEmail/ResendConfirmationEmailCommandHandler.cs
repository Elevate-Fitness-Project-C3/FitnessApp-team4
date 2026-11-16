using IdentityService.Email;
using IdentityService.Security.Tokens;
namespace IdentityService.Features.Auth.ResendConfirmationEmail;
internal sealed class ResendConfirmationEmailCommandHandler(UserManager<ApplicationUser> users, AuthEmailQueue email) : IRequestHandler<ResendConfirmationEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed) await email.EnqueueAsync(user.Id, OtpPurpose.ConfirmEmail, cancellationToken);
        return Result.Success;
    }
}
