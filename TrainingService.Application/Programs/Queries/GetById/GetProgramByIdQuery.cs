using Ardalis.Result;
using MediatR;
using TrainingService.Application.Programs.Dtos;

namespace TrainingService.Application.Programs.Queries.GetById;

public sealed record GetProgramByIdQuery(Guid Id) : IRequest<Result<ProgramDetailsDto>>;
