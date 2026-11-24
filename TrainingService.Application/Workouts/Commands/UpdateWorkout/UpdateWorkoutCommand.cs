using Ardalis.Result;
using MediatR;
using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Application.Workouts.Commands.UpdateWorkout;

public record UpdateWorkoutCommand(
    Guid Id,
    string Title,
    WorkoutDifficulty Difficulty,
    int EstimatedDuration
) : IRequest<Result>;