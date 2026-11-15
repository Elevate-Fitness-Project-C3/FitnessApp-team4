namespace IdentityService.Features.Users.AssignUserRole;

internal sealed class AssignUserRoleCommandHandler(
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<AssignUserRoleCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(AssignUserRoleCommand request,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await userManager.AddToRoleAsync(request.User,request.RoleName);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(error => Error.Validation(
                    code: $"Identity.{error.Code}",
                    description: error.Description))
                .Distinct()
                .ToList();

            return Result<Success>.Failure(errors);
        }

        return Result.Success;
    }
}