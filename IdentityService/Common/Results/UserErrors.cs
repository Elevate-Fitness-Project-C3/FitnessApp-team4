
namespace IdentityService.Common.Results;

public static class UserErrors
{
    public static readonly Error EmailAlreadyExists =
       Error.AlreadyExists(
           code: "Users.EmailAlreadyExists",
           description: "The email address is already registered.");
}
