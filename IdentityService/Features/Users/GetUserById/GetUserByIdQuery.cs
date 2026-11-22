using IdentityService.Features.Users.ListUsers;

namespace IdentityService.Features.Users.GetUserById
{
    public sealed record GetUserByIdQuery(string UserId) : IRequest<Result<UserDto>>;
}