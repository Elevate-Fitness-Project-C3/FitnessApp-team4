using IdentityService.Features.Roles;
using IdentityService.Security.Authorization;

namespace IdentityService.Features.Users;

public sealed record GetUserRolesQuery(string UserId) : IRequest<Result<UserRolesDto>>;
public sealed record GetUserPermissionsQuery(string UserId) : IRequest<Result<string[]>>;
public sealed class UserSecurityQueries(AuthDbContext db, EffectivePermissionResolver permissions)
    : IRequestHandler<GetUserRolesQuery, Result<UserRolesDto>>, IRequestHandler<GetUserPermissionsQuery, Result<string[]>>
{
    public async Task<Result<UserRolesDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return SecurityMutationChecks.Missing;
        var roles = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id).OrderBy(x => x.RoleId).Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        return Result<UserRolesDto>.Success(new(user.Id, user.ConcurrencyStamp!, roles));
    }
    public async Task<Result<string[]>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken)) return SecurityMutationChecks.Missing;
        return Result<string[]>.Success((await permissions.ResolveAsync(request.UserId, cancellationToken)).Order(StringComparer.Ordinal).ToArray());
    }
}
