using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Programs.Dtos;
using TrainingService.Domain.Programs;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Queries.GetById;

public sealed class GetProgramByIdQueryHandler : IRequestHandler<GetProgramByIdQuery, Result<ProgramDetailsDto>>
{
    private readonly IGenericRepository<Program> _programRepository;

    public GetProgramByIdQueryHandler(IGenericRepository<Program> programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<Result<ProgramDetailsDto>> Handle(
        GetProgramByIdQuery request,
        CancellationToken cancellationToken)
    {
        var program = await _programRepository
            .Get(p => p.Id == request.Id && !p.IsDeleted && p.Status == ProgramStatus.Published)
            .AsNoTracking()
            .Select(p => new ProgramDetailsDto(
                p.Id,
                p.Goal,
                p.Level,
                p.DurationWeeks,
                p.DaysPerWeek,
                p.AccessTier,
                p.Description,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt,
                p.WorkoutSlots
                    .Where(slot => !slot.IsDeleted && !slot.Workout.IsDeleted)
                    .OrderBy(slot => slot.WeekNumber)
                    .ThenBy(slot => slot.DayNumber)
                    .Select(slot => new ProgramWorkoutDto(
                        slot.Id,
                        slot.WorkoutId,
                        slot.Workout.Title,
                        slot.Workout.Difficulty,
                        slot.Workout.EstimatedDuration,
                        slot.WeekNumber,
                        slot.DayNumber
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (program is null)
        {
            return Result<ProgramDetailsDto>.NotFound($"Program with id '{request.Id}' was not found.");
        }

        return Result<ProgramDetailsDto>.Success(program);
    }
}
