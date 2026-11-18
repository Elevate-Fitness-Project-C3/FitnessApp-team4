using IdentityService.Security.Authorization;

namespace IdentityService.Features.UserRoles.GetUserRoles;

public sealed class GetUserRolesQueryHandler(AuthDbContext db)
    : IRequestHandler<GetUserRolesQuery, Result<UserRolesDto>>
{
    public async Task<Result<UserRolesDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return SecurityMutationChecks.Missing;
        var roles = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id).OrderBy(x => x.RoleId).Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        return Result<UserRolesDto>.Success(new(user.Id, user.ConcurrencyStamp!, roles));
    }
}
