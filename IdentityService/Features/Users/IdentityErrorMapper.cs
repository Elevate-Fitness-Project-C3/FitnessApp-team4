namespace IdentityService.Features.Users;

internal static class IdentityErrorMapper
{
    public static Result<T> ToResult<T>(IdentityResult result) =>
        Result<T>.Failure(result.Errors.Select(Map).Distinct().ToList());

    private static Error Map(IdentityError error) => error.Code switch
    {
        nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)
            => UserErrors.EmailAlreadyExists,
        nameof(IdentityErrorDescriber.UserAlreadyInRole) or nameof(IdentityErrorDescriber.ConcurrencyFailure)
            => Error.Conflict($"Identity.{error.Code}", error.Description),
        nameof(IdentityErrorDescriber.DefaultError)
            => Error.Failure($"Identity.{error.Code}", "The operation failed."),
        _ => Error.Validation($"Identity.{error.Code}", error.Description)
    };
}
