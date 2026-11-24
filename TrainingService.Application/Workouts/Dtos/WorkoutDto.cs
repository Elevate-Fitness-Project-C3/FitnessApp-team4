using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Application.Workouts.Dtos;

public record WorkoutDto(
    Guid Id,
    string Title,
    WorkoutDifficulty Difficulty,
    int EstimatedDuration,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<WorkoutExerciseDto> WorkoutExercises
);