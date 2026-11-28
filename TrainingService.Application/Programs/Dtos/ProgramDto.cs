using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Dtos;

public record ProgramDto(
    Guid Id,
    ProgramGoal Goal,
    ProgramLevel Level,
    int DurationWeeks,
    int DaysPerWeek,
    AccessTier AccessTier,
    string Description,
    ProgramStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
