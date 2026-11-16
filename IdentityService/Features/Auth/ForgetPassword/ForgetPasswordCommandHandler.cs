using IdentityService.Email;
using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.ForgetPassword;
internal sealed class ForgetPasswordCommandHandler(UserManager<ApplicationUser> users, AuthEmailSender email) : IRequestHandler<ForgetPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && user.EmailConfirmed) await email.SendAsync(user, reset: true);
        return Result.Success;
    }
}
