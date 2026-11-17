using IdentityService.Persistence.Seeding;
using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;
using MassTransit;

namespace IdentityService.Features.Roles;

public sealed class SecurityMutationChecks(AuthDbContext db, EffectivePermissionResolver permissions, AuditActorContext actor, IServiceProvider services)
{
    public async Task<Error?> AuthorizeAsync(string permission, CancellationToken cancellationToken)
    {
        if (services.GetService<ISendEndpointProvider>() is null)
            return Error.Unavailable("Audit.Disabled", "Audit messaging must be provisioned and enabled.");
        await SecurityAdministrationLock.AcquireAsync(db, cancellationToken);
        return actor.UserId is not null && (await permissions.ResolveAsync(actor.UserId, cancellationToken, fresh: true)).Contains(permission)
            ? null : Error.Forbidden();
    }

    public async Task<bool> CanManageRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        var own = await permissions.ResolveAsync(actor.UserId!, cancellationToken, fresh: true);
        var active = await db.Set<Permission>().Where(x => x.IsActive).Select(x => x.Code).ToListAsync(cancellationToken);
        return (await permissions.RolePermissionsAsync(roleId, cancellationToken)).Where(active.Contains).All(own.Contains);
    }

    public async Task<bool> HasRecoveryAdministratorAsync(CancellationToken cancellationToken)
    {
        var required = PermissionCatalog.RecoveryPermissions;
        var grants = await (from user in db.Users.AsNoTracking()
            join assignment in db.Set<IdentityUserRole<string>>() on user.Id equals assignment.UserId
            join claim in db.Set<IdentityRoleClaim<string>>() on assignment.RoleId equals claim.RoleId
            join definition in db.Set<Permission>() on claim.ClaimValue equals definition.Code
            where user.IsActive && user.EmailConfirmed && claim.ClaimType == "permission" && definition.IsActive && required.Contains(definition.Code)
            select new { user.Id, definition.Code }).ToListAsync(cancellationToken);
        return grants.GroupBy(x => x.Id).Any(group => required.All(code => group.Any(x => x.Code == code)));
    }

    public static bool IsDefaultRole(IdentityRole role) => string.Equals(role.Name, RoleNames.User, StringComparison.OrdinalIgnoreCase);
    public static readonly Error Conflict = Error.Conflict("Security.Concurrency", "The resource changed. Reload before retrying.");
    public static readonly Error Missing = Error.NotFound("Security.NotFound", "The resource was not found.");
    public static readonly Error Recovery = Error.Conflict("Security.LastAdministrator", "The last security administrator must remain active and authorized.");
}
