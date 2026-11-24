using Ardalis.Result;
using MediatR;
using TrainingService.Application.Workouts.Dtos;

namespace TrainingService.Application.Workouts.Queries.GetAll;

public record GetAllWorkoutsQuery
    : IRequest<Result<IReadOnlyList<WorkoutDto>>>;