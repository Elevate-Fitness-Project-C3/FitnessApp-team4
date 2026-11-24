using Ardalis.Result;
using MediatR;
using TrainingService.Application.Workouts.Dtos;

namespace TrainingService.Application.Workouts.Queries.GetById;

public record GetWorkoutByIdQuery(Guid Id)
    : IRequest<Result<WorkoutDto>>;