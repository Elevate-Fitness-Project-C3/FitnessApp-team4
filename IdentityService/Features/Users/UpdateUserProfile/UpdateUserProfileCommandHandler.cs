using IdentityService.Features.Users.ListUsers;
using IdentityService.Security.Auditing;
using IdentityService.Security.Authorization;

namespace IdentityService.Features.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileCommandHandler(
    AuthDbContext db, UserManager<ApplicationUser> users,
    SecurityMutationChecks checks, SecurityAuditWriter audit, SecurityMutationOrchestrator orchestrator)
    : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    public Task<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync("Identity.Users.Profile.Update", () => ApplyAsync(request, cancellationToken),
            cancellationToken);

    private async Task<Result<UserDto>> ApplyAsync(UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        await UserRowLock.AcquireAsync(db, request.UserId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return SecurityMutationChecks.Missing;
        var roleIds = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        foreach (var roleId in roleIds)
            if (!await checks.CanManageRoleAsync(roleId, cancellationToken))
                return Error.Forbidden();
        var oldStamp = user.ConcurrencyStamp;
        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;
        var oldPhone = user.PhoneNumber;
        var result = await ProfileMutation.ApplyAsync(users, user, request.FirstName, request.LastName,
            request.PhoneNumber, request.ConcurrencyToken);
        if (result.IsSuccess && oldStamp != user.ConcurrencyStamp)
        {
            var changedFields = new List<string>();
            if (oldFirstName != user.FirstName) changedFields.Add("firstName");
            if (oldLastName != user.LastName) changedFields.Add("lastName");
            if (oldPhone != user.PhoneNumber) changedFields.Add("phoneNumber");
            await audit.RecordAsync(AuditActions.UserProfileUpdated, "User", user.Id, null,
                SecurityAuditWriter.Values("changedFields", changedFields), cancellationToken);
        }

        return result;
    }
}
