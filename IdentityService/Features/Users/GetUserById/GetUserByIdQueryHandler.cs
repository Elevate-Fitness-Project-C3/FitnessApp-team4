using IdentityService.Features.Users.ListUsers;

namespace IdentityService.Features.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(AuthDbContext db) : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(x => x.Id == request.UserId)
            .Select(x => new UserDto(x.Id, x.FirstName, x.LastName, x.Email, x.EmailConfirmed, x.PhoneNumber,
                x.IsActive, x.CreatedAt, x.ConcurrencyStamp!))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null) return Error.NotFound("Users.NotFound", "User not found.");

        return user;
    }
}