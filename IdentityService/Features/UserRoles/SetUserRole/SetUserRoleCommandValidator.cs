namespace IdentityService.Features.UserRoles.SetUserRole;

public sealed class SetUserRoleCommandValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleCommandValidator() { RuleFor(x => x.ConcurrencyToken).NotEmpty().MaximumLength(128); }
}
