using IdentityService.Features.Roles;

namespace IdentityService.Features.Permissions;

public sealed record PermissionNode(string Code, string DisplayNameAr, string Kind, int DisplayOrder, bool IsActive, bool IsSelected, IReadOnlyList<PermissionNode> Children);
public sealed record PermissionTreeDto(string? RoleId, string? ConcurrencyToken, IReadOnlyList<PermissionNode> Nodes);
public sealed record GetPermissionTreeQuery(string? RoleId) : IRequest<Result<PermissionTreeDto>>;
public sealed class GetPermissionTreeHandler(AuthDbContext db) : IRequestHandler<GetPermissionTreeQuery, Result<PermissionTreeDto>>
{
    public async Task<Result<PermissionTreeDto>> Handle(GetPermissionTreeQuery request, CancellationToken cancellationToken)
    {
        IdentityRole? role = null;
        var selected = new HashSet<string>(StringComparer.Ordinal);
        if (request.RoleId is not null)
        {
            role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
            if (role is null) return SecurityMutationChecks.Missing;
            selected = (await db.Set<IdentityRoleClaim<string>>().Where(x => x.RoleId == role.Id && x.ClaimType == "permission").Select(x => x.ClaimValue!).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        }
        var groups = await db.Set<PermissionGroup>().AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
        var definitions = await db.Set<Permission>().AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
        IReadOnlyList<PermissionNode> Build(string? parent) => groups.Where(x => x.ParentCode == parent).Select(group =>
            new PermissionNode(group.Code, group.DisplayNameAr, "group", group.DisplayOrder, true, false,
                Build(group.Code).Concat(definitions.Where(x => x.GroupCode == group.Code).Select(permission =>
                    new PermissionNode(permission.Code, permission.DisplayNameAr, "permission", permission.DisplayOrder, permission.IsActive, selected.Contains(permission.Code), []))).ToArray())).ToArray();
        return Result<PermissionTreeDto>.Success(new(role?.Id, role?.ConcurrencyStamp, Build(null)));
    }
}
