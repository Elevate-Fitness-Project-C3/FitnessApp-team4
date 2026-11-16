using IdentityService.Email;
using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ResendConfirmationEmail;
internal sealed class ResendConfirmationEmailCommandHandler(UserManager<ApplicationUser> users, AuthEmailSender email) : IRequestHandler<ResendConfirmationEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed) await email.SendAsync(user);
        return Result.Success;
    }
}
