using IdentityService.Features.Auth.Register;

namespace IdentityService.Features.Users.CheckEmailExists;

internal sealed class CheckEmailExistsQueryHandler(UserManager<ApplicationUser> userManager): IRequestHandler<CheckEmailExistsQuery, Result<bool>>
{
    async Task<Result<bool>> IRequestHandler<CheckEmailExistsQuery, Result<bool>>.Handle(CheckEmailExistsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim();

        var existingUser = await userManager.FindByEmailAsync(email);

        return Result<bool>.Success(existingUser is not null);
    }
}
