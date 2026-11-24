using TrainingService.Domain.Exercises.Enums;

namespace TrainingService.Application.Exercises.Dtos;

public record ExerciseDto(
    Guid Id,
    string Name,
    string? Description,
    string Instructions,
    string TargetMuscleGroup,
    ExerciseTrackingType TrackingType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);