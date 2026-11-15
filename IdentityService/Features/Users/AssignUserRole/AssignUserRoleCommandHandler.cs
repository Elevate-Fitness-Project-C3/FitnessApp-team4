
namespace IdentityService.Features.Users.AssignUserRole;

internal sealed class AssignUserRoleCommandHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<AssignUserRoleCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await userManager.AddToRoleAsync(request.User, request.RoleName);
        if (!result.Succeeded)
            return MapAssignmentErrors(result);
        return Result.Success;
    }
    private static Result<Success> MapAssignmentErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors
            .Select(error => error.Code switch
            {
                nameof(IdentityErrorDescriber.UserAlreadyInRole) or
                nameof(IdentityErrorDescriber.ConcurrencyFailure)
                    => Error.Conflict($"Identity.{error.Code}", error.Description),
                nameof(IdentityErrorDescriber.DefaultError)
                    => Error.Failure($"Identity.{error.Code}", "Failed to assign the role."),
                _ => Error.Validation($"Identity.{error.Code}", error.Description)
            })
            .Distinct()
            .ToList();
        return Result<Success>.Failure(errors);
    }
}
