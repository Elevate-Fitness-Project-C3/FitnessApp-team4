using IdentityService.Security.Authorization;

namespace IdentityService.Features.Roles.GetRole;

public sealed class GetRoleQueryHandler(AuthDbContext db, EffectivePermissionResolver permissions)
    : IRequestHandler<GetRoleQuery, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (role is null) return SecurityMutationChecks.Missing;
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, (await permissions.RolePermissionsAsync(role.Id, cancellationToken)).Order(StringComparer.Ordinal).ToArray()));
    }
}
