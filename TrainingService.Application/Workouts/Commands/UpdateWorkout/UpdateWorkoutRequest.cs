using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Application.Workouts.Commands.UpdateWorkout;

public record UpdateWorkoutRequest(
    string Title,
    WorkoutDifficulty Difficulty,
    int EstimatedDuration
);