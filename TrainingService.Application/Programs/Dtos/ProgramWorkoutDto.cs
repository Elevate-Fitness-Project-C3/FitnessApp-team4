using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Application.Programs.Dtos;

public record ProgramWorkoutDto(
    Guid Id,
    Guid WorkoutId,
    string Title,
    WorkoutDifficulty Difficulty,
    int EstimatedDuration,
    int WeekNumber,
    int DayNumber
);
