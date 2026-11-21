using Ardalis.Result;
using MediatR;
using TrainingService.Application.Exercises.Dtos;

namespace TrainingService.Application.Exercises.Queries.GetById;

public record GetExerciseByIdQuery(Guid Id)
    : IRequest<Result<ExerciseDto>>;