using IdentityService.Features.Users.ListUsers;
using IdentityService.Features.Users.UpdateUserProfile;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Users.UpdateOwnProfile;

internal sealed class UpdateOwnProfileCommandHandler(AuthDbContext db, UserManager<ApplicationUser> users, AuditActorContext actor)
    : IRequestHandler<UpdateOwnProfileCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateOwnProfileCommand request, CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId) return Error.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await UserRowLock.AcquireAsync(db, userId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive) return Error.Unauthorized();
        var result = await ProfileMutation.ApplyAsync(users, user, request.FirstName, request.LastName,
            request.PhoneNumber, request.ConcurrencyToken);
        if (result.IsSuccess) await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
