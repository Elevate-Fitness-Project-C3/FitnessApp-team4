namespace IdentityService.Features.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.ConcurrencyToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PermissionCodes).Cascade(CascadeMode.Stop).NotNull().Must(x => x.Length <= 500 && x.Distinct(StringComparer.Ordinal).Count() == x.Length);
        RuleForEach(x => x.PermissionCodes).NotEmpty().MaximumLength(160);
    }
}
