namespace IdentityService.Features.Roles.ListRoles;

public sealed class ListRolesQueryHandler(AuthDbContext db)
    : IRequestHandler<ListRolesQuery, Result<PageDto<RoleDto>>>
{
    public async Task<Result<PageDto<RoleDto>>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        if (request.Page is < 1 or > 100000 || request.PageSize is < 1 or > 100 || request.Search?.Length > 256)
            return Error.Validation("Roles.Pagination", "Invalid role search or pagination.");
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.Name!.Contains(request.Search));
        var total = await query.CountAsync(cancellationToken);
        var roles = await query.OrderBy(x => x.NormalizedName).ThenBy(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        var ids = roles.Select(x => x.Id).ToArray();
        var claims = await db.Set<IdentityRoleClaim<string>>().AsNoTracking().Where(x => ids.Contains(x.RoleId) && x.ClaimType == "permission").ToListAsync(cancellationToken);
        return Result<PageDto<RoleDto>>.Success(new(roles.Select(role => new RoleDto(role.Id, role.Name!, role.ConcurrencyStamp!, claims.Where(x => x.RoleId == role.Id).Select(x => x.ClaimValue!).Distinct().Order(StringComparer.Ordinal).ToArray())).ToArray(), request.Page, request.PageSize, total));
    }
}
