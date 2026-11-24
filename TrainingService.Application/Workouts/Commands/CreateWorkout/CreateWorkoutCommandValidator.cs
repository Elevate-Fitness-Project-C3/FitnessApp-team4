using FluentValidation;

namespace TrainingService.Application.Workouts.Commands.CreateWorkout;


public class CreateWorkoutCommandValidator
    : AbstractValidator<CreateWorkoutCommand>
{
    public CreateWorkoutCommandValidator()
    {
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