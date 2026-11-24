using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;

public record AddExerciseToWorkoutOrchestrator(
    Guid WorkoutId,
    Guid ExerciseId,
    int Order,
    int? Sets,
    int? Reps,
    int? Duration
) : IRequest<Result<Guid>>;