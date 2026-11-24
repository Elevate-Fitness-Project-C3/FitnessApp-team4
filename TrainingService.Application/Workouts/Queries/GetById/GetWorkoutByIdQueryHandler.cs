using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Workouts.Dtos;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Queries.GetById;

public class GetWorkoutByIdQueryHandler
    : IRequestHandler<GetWorkoutByIdQuery, Result<WorkoutDto>>
{
    private readonly IGenericRepository<Workout> _repository;

    public GetWorkoutByIdQueryHandler(
        IGenericRepository<Workout> repository)
    {
        _repository = repository;
    }

    public async Task<Result<WorkoutDto>> Handle(
        GetWorkoutByIdQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _repository
            .Get(workout =>
                workout.Id == request.Id &&
                !workout.IsDeleted)
            .Select(workout => new WorkoutDto(
                workout.Id,
                workout.Title,
                workout.Difficulty,
                workout.EstimatedDuration,
                workout.CreatedAt,
                workout.UpdatedAt,
                workout.WorkoutExercises
                    .Where(we => !we.IsDeleted && !we.Exercise.IsDeleted)
                    .OrderBy(we => we.Order)
                    .Select(we => new WorkoutExerciseDto(
                        we.Id,
                        we.ExerciseId,
                        we.Exercise.Name,
                        we.Order,
                        we.Sets,
                        we.Reps,
                        we.Duration
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result<WorkoutDto>.NotFound("Workout not found.");
        }

        return Result<WorkoutDto>.Success(response);
    }
}