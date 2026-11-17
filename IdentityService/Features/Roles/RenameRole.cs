using IdentityService.Features.Users;
using IdentityService.Security.Authorization;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles;

public sealed record RenameRoleCommand(string RoleId, string Name, string ConcurrencyToken) : IRequest<Result<RoleDto>>;
public sealed class RenameRoleValidator : AbstractValidator<RenameRoleCommand>
{
    public RenameRoleValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(256).Must(x => x == x.Trim() && !x.Any(char.IsControl));
        RuleFor(x => x.ConcurrencyToken).NotEmpty().MaximumLength(128);
    }
}
public sealed class RenameRoleHandler(AuthDbContext db, RoleManager<IdentityRole> roles, SecurityMutationChecks checks, SecurityAuditWriter audit, EffectivePermissionResolver permissions)
    : IRequestHandler<RenameRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(RenameRoleCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync("Identity.Roles.Rename", cancellationToken);
        if (error is not null) return error.Value;
        var role = await roles.FindByIdAsync(request.RoleId);
        if (role is null) return SecurityMutationChecks.Missing;
        if (role.ConcurrencyStamp != request.ConcurrencyToken) return SecurityMutationChecks.Conflict;
        if (SecurityMutationChecks.IsDefaultRole(role) || !await checks.CanManageRoleAsync(role.Id, cancellationToken)) return Error.Forbidden();
        var grants = (await permissions.RolePermissionsAsync(role.Id, cancellationToken)).Order(StringComparer.Ordinal).ToArray();
        if (role.Name == request.Name) return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, grants));
        var oldName = role.Name;
        var result = await roles.SetRoleNameAsync(role, request.Name);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        result = await roles.UpdateAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        await audit.RecordAsync("Identity.RoleRenamed", "Role", role.Id, SecurityAuditWriter.Values("name", oldName), SecurityAuditWriter.Values("name", role.Name), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, grants));
    }
}
