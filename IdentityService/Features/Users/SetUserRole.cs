using IdentityService.Features.Roles;
using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;
using IdentityService.Security.Tokens;

namespace IdentityService.Features.Users;

public sealed record SetUserRoleCommand(string UserId, string RoleId, bool Assign, string ConcurrencyToken) : IRequest<Result<UserRolesDto>>;
public sealed class SetUserRoleValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleValidator() { RuleFor(x => x.ConcurrencyToken).NotEmpty().MaximumLength(128); }
}
public sealed class SetUserRoleHandler(AuthDbContext db, UserManager<ApplicationUser> users, SecurityMutationChecks checks, SecurityAuditWriter audit)
    : IRequestHandler<SetUserRoleCommand, Result<UserRolesDto>>
{
    public async Task<Result<UserRolesDto>> Handle(SetUserRoleCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync(request.Assign ? "Identity.Users.Roles.Assign" : "Identity.Users.Roles.Remove", cancellationToken);
        if (error is not null) return error.Value;
        await RefreshTokenSessionLock.AcquireAsync(db, request.UserId, cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (user is null || role is null) return SecurityMutationChecks.Missing;
        if (user.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (!await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        var assigned = await db.Set<IdentityUserRole<string>>().AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken);
        if (assigned != request.Assign)
        {
            var result = request.Assign ? await users.AddToRoleAsync(user, role.Name!) : await users.RemoveFromRoleAsync(user, role.Name!);
            if (!result.Succeeded) return IdentityErrorMapper.ToResult<UserRolesDto>(result);
            if (!await checks.HasRecoveryAdministratorAsync(cancellationToken)) return SecurityMutationChecks.Recovery;
            await audit.RecordAsync(request.Assign ? "Identity.RoleAssigned" : "Identity.RoleRemoved", "User", user.Id,
                request.Assign ? null : SecurityAuditWriter.Values("roleId", role.Id),
                request.Assign ? SecurityAuditWriter.Values("roleId", role.Id) : null, cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        var roles = await db.Set<IdentityUserRole<string>>().Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToArrayAsync(cancellationToken);
        return Result<UserRolesDto>.Success(new(user.Id, user.ConcurrencyStamp!, roles));
    }
}
