using IdentityService.Features.Users;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles;

public sealed record DeleteRoleCommand(string RoleId, string ConcurrencyToken) : IRequest<Result<Success>>;
public sealed class DeleteRoleHandler(AuthDbContext db, RoleManager<IdentityRole> roles, SecurityMutationChecks checks, SecurityAuditWriter audit)
    : IRequestHandler<DeleteRoleCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync("Identity.Roles.Delete", cancellationToken);
        if (error is not null) return error.Value;
        var role = await roles.FindByIdAsync(request.RoleId);
        if (role is null) return SecurityMutationChecks.Missing;
        if (role.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (SecurityMutationChecks.IsDefaultRole(role) || !await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        if (await db.Set<IdentityUserRole<string>>().AnyAsync(x => x.RoleId == role.Id, cancellationToken))
            return Error.Conflict("Role.Assigned", "Remove role assignments before deleting the role.");
        var result = await roles.DeleteAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<Success>(result);
        await audit.RecordAsync("Identity.RoleDeleted", "Role", role.Id, SecurityAuditWriter.Values("name", role.Name), null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success;
    }
}
