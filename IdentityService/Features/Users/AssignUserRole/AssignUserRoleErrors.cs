namespace IdentityService.Features.Users.AssignUserRole;

internal static class AssignUserRoleErrors
{
    public static readonly Error UserNotFound =
    Error.NotFound(
        code: "AssignUserRole.UserNotFound",
        description: "The specified user was not found.");
}
