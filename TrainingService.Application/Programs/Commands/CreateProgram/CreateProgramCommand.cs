using Ardalis.Result;
using MediatR;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Commands.CreateProgram;

public record CreateProgramCommand(
    ProgramGoal Goal,
    ProgramLevel Level,
    int DurationWeeks,
    int DaysPerWeek,
    AccessTier AccessTier,
    string Description
) : IRequest<Result<Guid>>;
