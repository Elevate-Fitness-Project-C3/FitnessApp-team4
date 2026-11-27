using FluentValidation;

namespace TrainingService.Application.UserTrainingProgram.Commands.CreateUserTrainingProgram;

public class CreateUserTrainingProgramCommandValidator
    : AbstractValidator<CreateUserTrainingProgramCommand>
{
    public CreateUserTrainingProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required.");
    }
}