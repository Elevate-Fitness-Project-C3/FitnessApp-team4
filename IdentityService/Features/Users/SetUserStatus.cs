using IdentityService.Features.Roles;
using IdentityService.Security.Auditing;
using IdentityService.Security.Tokens;

namespace IdentityService.Features.Users;

public sealed record SetUserStatusCommand(string UserId, bool IsActive, string ConcurrencyToken) : IRequest<Result<Success>>;
public sealed class SetUserStatusHandler(AuthDbContext db, UserManager<ApplicationUser> users, SecurityMutationChecks checks, SecurityAuditWriter audit)
    : IRequestHandler<SetUserStatusCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync("Identity.Users.Update", cancellationToken);
        if (error is not null) return error.Value;
        await RefreshTokenSessionLock.AcquireAsync(db, request.UserId, cancellationToken);
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
        await audit.RecordAsync("Identity.UserStatusUpdated", "User", user.Id, SecurityAuditWriter.Values("isActive", old), SecurityAuditWriter.Values("isActive", user.IsActive), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success;
    }
}
