using IdentityService.Security.Authorization;
using IdentityService.Persistence.Seeding;
using IdentityService.Security.Auditing;
using IdentityService.Security.Tokens;

namespace IdentityService.Operations;

public static class SecurityBootstrap
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var checks = scope.ServiceProvider.GetRequiredService<SecurityMutationChecks>();
        var applicationId = scope.ServiceProvider.GetRequiredService<SecurityAuditScope>().ApplicationId;
        var id = configuration["SecurityBootstrap:UserId"];
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Set SecurityBootstrap:UserId to an existing confirmed, active user.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SecurityAdministrationLock.AcquireAsync(db, cancellationToken);
        if (await checks.HasRecoveryAdministratorAsync(cancellationToken)) throw new InvalidOperationException("An authorized security administrator already exists. Bootstrap cannot overwrite security configuration.");
        await RefreshTokenSessionLock.AcquireAsync(db, id, cancellationToken);
        var user = await users.FindByIdAsync(id);
        if (user is null || !user.IsActive || !user.EmailConfirmed) throw new InvalidOperationException("Bootstrap requires an existing confirmed, active user.");
        const string name = "SecurityAdministrator";
        var role = await roles.FindByNameAsync(name);
        if (role is null)
        {
            role = new IdentityRole(name);
            if (!(await roles.CreateAsync(role)).Succeeded) throw new InvalidOperationException("Security bootstrap role creation failed.");
        }
        var existing = await db.Set<IdentityRoleClaim<string>>().Where(x => x.RoleId == role.Id && x.ClaimType == "permission").Select(x => x.ClaimValue).ToListAsync(cancellationToken);
        foreach (var (code, _) in PermissionCatalog.Definitions)
            if (!existing.Contains(code)) db.Add(new IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = "permission", ClaimValue = code });
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        await db.SaveChangesAsync(cancellationToken);
        if (!await users.IsInRoleAsync(user, name) && !(await users.AddToRoleAsync(user, name)).Succeeded) throw new InvalidOperationException("Security bootstrap role assignment failed.");
        if (!await db.Set<AuditReportGrant>().AnyAsync(x => x.UserId == user.Id && x.ApplicationId == applicationId && x.TenantId == null && x.SourceService == SecurityAuditScope.SourceService, cancellationToken))
            db.Add(new AuditReportGrant { Id = Guid.NewGuid(), UserId = user.Id, ApplicationId = applicationId, SourceService = SecurityAuditScope.SourceService });
        await scope.ServiceProvider.GetRequiredService<SecurityAuditWriter>().RecordAsync("Identity.SecurityBootstrap", "User", user.Id, null, SecurityAuditWriter.Values("roleId", role.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
