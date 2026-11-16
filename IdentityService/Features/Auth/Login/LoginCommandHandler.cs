using IdentityService.Email;
using IdentityService.Security.Tokens;
using IdentityService.Features.Auth.Login;
namespace IdentityService.Features.Auth.Login;
internal sealed class LoginCommandHandler(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, RefreshTokenIssuer issuer) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null) return AuthErrors.InvalidCredentials;
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return result.IsNotAllowed ? AuthErrors.EmailNotConfirmed : AuthErrors.InvalidCredentials;
        return await issuer.IssueAsync(user, cancellationToken);
    }
}
