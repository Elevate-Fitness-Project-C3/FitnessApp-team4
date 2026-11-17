using IdentityService.Features.Users;
using IdentityService.Security.Auditing;

namespace IdentityService.Features.Roles;

public sealed record CreateRoleCommand(string Name) : IRequest<Result<RoleDto>>;
public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator() { RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(256).Must(x => x == x.Trim() && !x.Any(char.IsControl)); }
}
public sealed class CreateRoleHandler(AuthDbContext db, RoleManager<IdentityRole> roles, SecurityMutationChecks checks, SecurityAuditWriter audit)
    : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var error = await checks.AuthorizeAsync("Identity.Roles.Create", cancellationToken);
        if (error is not null) return error.Value;
        var role = new IdentityRole(request.Name);
        var result = await roles.CreateAsync(role);
        if (!result.Succeeded) return IdentityErrorMapper.ToResult<RoleDto>(result);
        await audit.RecordAsync("Identity.RoleCreated", "Role", role.Id, null, SecurityAuditWriter.Values("name", role.Name), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<RoleDto>.Success(new(role.Id, role.Name!, role.ConcurrencyStamp!, []));
    }
}
