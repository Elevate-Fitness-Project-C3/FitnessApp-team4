namespace IdentityService.Features.Roles.RenameRole;

public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
{
    public RenameRoleCommandValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(256).Must(x => x == x.Trim() && !x.Any(char.IsControl));
        RuleFor(x => x.ConcurrencyToken).NotEmpty().MaximumLength(128);
    }
}
