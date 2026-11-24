using FluentValidation;

namespace TrainingService.Application.Workouts.Commands.UpdateWorkout;

public class UpdateWorkoutCommandValidator
    : AbstractValidator<UpdateWorkoutCommand>
{
    public UpdateWorkoutCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Workout ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Workout title is required.")
            .MaximumLength(150)
            .WithMessage("Workout title must not exceed 150 characters.");

        RuleFor(x => x.Difficulty)
            .IsInEnum()
            .WithMessage("Invalid workout difficulty.");

        RuleFor(x => x.EstimatedDuration)
            .GreaterThan(0)
            .WithMessage("Estimated duration must be greater than zero.");
    }
}