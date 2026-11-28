using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Models;
using TrainingService.Application.Programs.Dtos;

namespace TrainingService.Application.Programs.Queries.GetAll;

public sealed record GetAllProgramsQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<ProgramDto>>>;
