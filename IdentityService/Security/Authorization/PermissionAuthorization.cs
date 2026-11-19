using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Authorization;

public sealed class HasPermissionAttribute(string code) : AuthorizeAttribute("permission:" + code);
public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        => policyName.StartsWith("permission:", StringComparison.Ordinal)
            ? Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(policyName[11..])).Build())
            : base.GetPolicyAsync(policyName);
}

public sealed class EffectivePermissionResolver(AuthDbContext db)
{
    private readonly Dictionary<string, HashSet<string>> requestCache = new(StringComparer.Ordinal);

    public async Task<HashSet<string>> ResolveAsync(string userId, CancellationToken cancellationToken, bool fresh = false)
    {
        if (!fresh && requestCache.TryGetValue(userId, out var cached)) return cached;
        var permissions = await (from user in db.Users.AsNoTracking()
            join assignment in db.Set<IdentityUserRole<string>>() on user.Id equals assignment.UserId
            join claim in db.Set<IdentityRoleClaim<string>>() on assignment.RoleId equals claim.RoleId
            join permission in db.Set<Permission>() on claim.ClaimValue equals permission.Code
            where user.Id == userId && user.IsActive && user.EmailConfirmed && claim.ClaimType == "permission" && permission.IsActive
            select permission.Code).Distinct().ToListAsync(cancellationToken);
        var result = permissions.ToHashSet(StringComparer.Ordinal);
        if (!fresh) requestCache[userId] = result;
        return result;
    }

    public Task<List<string>> RolePermissionsAsync(string roleId, CancellationToken cancellationToken)
        => db.Set<IdentityRoleClaim<string>>().Where(x => x.RoleId == roleId && x.ClaimType == "permission").Select(x => x.ClaimValue!).Distinct().ToListAsync(cancellationToken);
}

public sealed class PermissionAuthorizationHandler(EffectivePermissionResolver resolver, IHttpContextAccessor accessor)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var id = context.User.FindFirst("sub")?.Value;
        if (id is not null && (await resolver.ResolveAsync(id, accessor.HttpContext?.RequestAborted ?? default)).Contains(requirement.Code)) context.Succeed(requirement);
    }
}
