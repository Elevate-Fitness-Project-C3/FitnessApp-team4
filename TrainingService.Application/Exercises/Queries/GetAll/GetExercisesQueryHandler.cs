using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Queries.GetAll;

public class GetExercisesQueryHandler
    : IRequestHandler<GetExercisesQuery, Result<IReadOnlyList<ExerciseDto>>>
{
    private readonly IGenericRepository<Exercise> _repository;

    public GetExercisesQueryHandler(
        IGenericRepository<Exercise> repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ExerciseDto>>> Handle(
        GetExercisesQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _repository
            .Get(exercise => !exercise.IsDeleted)
            .Select(exercise => new ExerciseDto(
                exercise.Id,
                exercise.Name,
                exercise.Description,
                exercise.Instructions,
                exercise.TargetMuscleGroup,
                exercise.TrackingType,
                exercise.IsActive,
                exercise.CreatedAt,
                exercise.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExerciseDto>>.Success(response);
    }
}