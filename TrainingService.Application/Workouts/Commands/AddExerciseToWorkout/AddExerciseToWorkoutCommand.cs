using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;

public record AddExerciseToWorkoutCommand(
    Guid WorkoutId,
    Guid ExerciseId,
    int Order,
    int? Sets,
    int? Reps,
    int? Duration
) : IRequest<Result<Guid>>;