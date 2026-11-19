namespace IdentityService.Features.Auth.Register;

public sealed record RegisterUserCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<Result<RegisterUserResponse>>
{
    public override string ToString() => nameof(RegisterUserCommand);
}