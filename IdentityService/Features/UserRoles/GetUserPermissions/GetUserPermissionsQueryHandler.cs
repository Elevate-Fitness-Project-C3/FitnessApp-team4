using IdentityService.Security.Authorization;

namespace IdentityService.Features.UserRoles.GetUserPermissions;

public sealed class GetUserPermissionsQueryHandler(AuthDbContext db, EffectivePermissionResolver permissions)
    : IRequestHandler<GetUserPermissionsQuery, Result<string[]>>
{
    public async Task<Result<string[]>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken)) return SecurityMutationChecks.Missing;
        return Result<string[]>.Success((await permissions.ResolveAsync(request.UserId, cancellationToken)).Order(StringComparer.Ordinal).ToArray());
    }
}
