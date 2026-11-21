using FluentValidation;

namespace TrainingService.Application.Exercises.Commands.DeleteExercise;

public class DeleteExerciseCommandValidator
    : AbstractValidator<DeleteExerciseCommand>
{
    public DeleteExerciseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Exercise ID is required.");
    }
}