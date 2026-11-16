namespace IdentityService.Features.Auth.Register;

public sealed class RegisterUserCommandValidator
    : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.")
            .EmailAddress()
            .WithMessage("Email address is invalid.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters.");
    }
}
