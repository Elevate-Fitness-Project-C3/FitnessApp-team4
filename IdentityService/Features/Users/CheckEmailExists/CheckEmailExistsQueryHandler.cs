namespace IdentityService.Features.Users.CheckEmailExists;

internal sealed class CheckEmailExistsQueryHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<CheckEmailExistsQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(CheckEmailExistsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
        var exists = await userManager.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        return Result<bool>.Success(exists);
    }
}
