using Ardalis.Result;
using MediatR;
using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Application.Workouts.Commands.CreateWorkout;

public record CreateWorkoutCommand(
    string Title,
    WorkoutDifficulty Difficulty,
    int EstimatedDuration
) : IRequest<Result<Guid>>;