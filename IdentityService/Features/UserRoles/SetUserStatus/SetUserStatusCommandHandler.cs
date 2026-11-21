using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.UserRoles.SetUserStatus;

public sealed class SetUserStatusCommandHandler(AuthDbContext db, UserManager<ApplicationUser> users, SecurityMutationChecks checks, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<SetUserStatusCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Users.Update", () => ApplyAsync(request, cancellationToken), cancellationToken);

    private async Task<Result<Success>> ApplyAsync(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        await UserRowLock.AcquireAsync(db, request.UserId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return SecurityMutationChecks.Missing;
        if (user.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        var roleIds = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        foreach (var roleId in roleIds) if (!await checks.CanManageRoleAsync(roleId, cancellationToken)) return Error.Forbidden();
        if (user.IsActive == request.IsActive) return Result.Success;
        var old = user.IsActive;
        user.IsActive = request.IsActive;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<Success>(result);
        if (!request.IsActive)
        {
            var now = DateTime.UtcNow;
            await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedOn == null).ExecuteUpdateAsync(set => set.SetProperty(x => x.RevokedOn, now), cancellationToken);
        }
        if (!await checks.HasRecoveryAdministratorAsync(cancellationToken)) return SecurityMutationChecks.Recovery;
        await audit.RecordAsync(AuditActions.UserStatusUpdated, "User", user.Id, SecurityAuditWriter.Values("isActive", old), SecurityAuditWriter.Values("isActive", user.IsActive), cancellationToken);
        return Result.Success;
    }
}
