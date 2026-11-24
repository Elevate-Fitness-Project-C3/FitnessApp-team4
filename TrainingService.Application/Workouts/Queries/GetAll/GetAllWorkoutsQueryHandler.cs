using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Workouts.Dtos;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Queries.GetAll;

public class GetAllWorkoutsQueryHandler
    : IRequestHandler<GetAllWorkoutsQuery, Result<IReadOnlyList<WorkoutDto>>>
{
    private readonly IGenericRepository<Workout> _repository;

    public GetAllWorkoutsQueryHandler(
        IGenericRepository<Workout> repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<WorkoutDto>>> Handle(
        GetAllWorkoutsQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _repository
            .Get(workout => !workout.IsDeleted)
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
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WorkoutDto>>.Success(response);
    }
}