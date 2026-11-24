using FluentValidation;

namespace TrainingService.Application.Workouts.Commands.DeleteWorkout;

public class DeleteWorkoutCommandValidator
    : AbstractValidator<DeleteWorkoutCommand>
{
    public DeleteWorkoutCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Workout ID is required.");
    }
}