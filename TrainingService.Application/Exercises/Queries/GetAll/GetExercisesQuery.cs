using Ardalis.Result;
using MediatR;
using TrainingService.Application.Exercises.Dtos;

namespace TrainingService.Application.Exercises.Queries.GetAll;


public record GetExercisesQuery
    : IRequest<Result<IReadOnlyList<ExerciseDto>>>;