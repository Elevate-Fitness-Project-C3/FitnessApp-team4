namespace IdentityService.Features.AuditGrants.SetAuditGrant;

public sealed class SetAuditGrantCommandValidator : AbstractValidator<SetAuditGrantCommand>
{
    public SetAuditGrantCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.ApplicationId).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(100)
            .Must(x => x.Trim() == x && !x.Any(char.IsControl))
            .Must(x => x.All(c => c is >= ' ' and <= '~')).When(x => !x.Remove, ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.SourceService).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(64)
            .Must(x => x.Trim() == x && !x.Any(char.IsControl))
            .Must(x => x.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
            .When(x => !x.Remove, ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.TenantId).MaximumLength(128).Must(x => x is null || x.Length > 0 && x.Trim() == x && !x.Any(char.IsControl));
    }
}
