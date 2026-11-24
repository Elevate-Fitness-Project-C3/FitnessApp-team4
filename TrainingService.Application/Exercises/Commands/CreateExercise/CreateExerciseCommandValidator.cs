using FluentValidation;

namespace TrainingService.Application.Exercises.Commands.CreateExercise;

public class CreateExerciseCommandValidator
    : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Instructions)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.TargetMuscleGroup)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TrackingType)
            .IsInEnum();
    }
}