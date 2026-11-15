namespace IdentityService.Features.Users.CreateUser;

internal sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<Result<ApplicationUser>>;