using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Common.Models;
using TrainingService.Application.Programs.Dtos;
using TrainingService.Domain.Programs;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.Programs.Queries.GetAll;

public sealed class GetAllProgramsQueryHandler
    : IRequestHandler<GetAllProgramsQuery, Result<PaginatedResult<ProgramDto>>>
{
    private const int MaxPageSize = 100;

    private readonly IGenericRepository<Program> _programRepository;

    public GetAllProgramsQueryHandler(IGenericRepository<Program> programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<Result<PaginatedResult<ProgramDto>>> Handle(
        GetAllProgramsQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new List<ValidationError>();

        if (request.PageNumber < 1)
        {
            validationErrors.Add(new ValidationError(
                nameof(request.PageNumber),
                "PageNumber must be greater than or equal to 1."));
        }

        if (request.PageSize < 1 || request.PageSize > MaxPageSize)
        {
            validationErrors.Add(new ValidationError(
                nameof(request.PageSize),
                $"PageSize must be between 1 and {MaxPageSize}."));
        }

        if (validationErrors.Count > 0)
        {
            return Result<PaginatedResult<ProgramDto>>.Invalid(validationErrors);
        }

        var query = _programRepository
            .Get(program => !program.IsDeleted && program.Status == ProgramStatus.Published)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(program => program.CreatedAt)
            .ThenBy(program => program.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(program => new ProgramDto(
                program.Id,
                program.Goal,
                program.Level,
                program.DurationWeeks,
                program.DaysPerWeek,
                program.AccessTier,
                program.Description,
                program.Status,
                program.CreatedAt,
                program.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        var paginatedResult = PaginatedResult<ProgramDto>.Create(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PaginatedResult<ProgramDto>>.Success(paginatedResult);
    }
}
