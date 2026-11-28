namespace TrainingService.Application.UserTrainingProgram.Dtos;

public record UserTrainingProgramDto(
    Guid Id,
    Guid ProgramId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    DateTime? StoppedAt
);