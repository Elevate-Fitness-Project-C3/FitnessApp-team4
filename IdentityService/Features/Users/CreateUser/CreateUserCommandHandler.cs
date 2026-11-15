
using Microsoft.Data.SqlClient;
namespace IdentityService.Features.Users.CreateUser;

internal sealed class CreateUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<CreateUserCommand, Result<ApplicationUser>>
{
    public async Task<Result<ApplicationUser>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            UserName = email,
            EmailConfirmed = false
        };
        try
        {
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return MapCreationErrors(result);
            return Result<ApplicationUser>.Success(user);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmailViolation(exception))
        {
            foreach (var entry in exception.Entries)
            {
                if (ReferenceEquals(entry.Entity, user))
                    entry.State = EntityState.Detached;
            }
            return UserErrors.EmailAlreadyExists;
        }
    }
    private static Result<ApplicationUser> MapCreationErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors
            .Select(error => error.Code switch
            {
                nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)
                    => UserErrors.EmailAlreadyExists,
                _ => Error.Validation(
                    code: $"Identity.{error.Code}",
                    description: error.Description)
            })
            .Distinct()
            .ToList();
        return Result<ApplicationUser>.Failure(errors);
    }
    private static bool IsDuplicateEmailViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && sqlException.Number is 2601 or 2627
            && (sqlException.Message.Contains("EmailIndex", StringComparison.OrdinalIgnoreCase)
                || sqlException.Message.Contains("UserNameIndex", StringComparison.OrdinalIgnoreCase));
    }
}
