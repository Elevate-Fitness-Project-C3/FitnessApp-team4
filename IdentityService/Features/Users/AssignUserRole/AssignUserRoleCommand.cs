namespace IdentityService.Features.Users.AssignUserRole;

internal sealed record AssignUserRoleCommand(
    ApplicationUser User,
    string RoleName
) : IRequest<Result<Success>>;