namespace IdentityService.Features.Roles.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator() { RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(256).Must(x => x == x.Trim() && !x.Any(char.IsControl)); }
}
