using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Queries.GetById;

public class GetExerciseByIdQueryHandler
    : IRequestHandler<GetExerciseByIdQuery, Result<ExerciseDto>>
{
    private readonly IGenericRepository<Exercise> _repository;

    public GetExerciseByIdQueryHandler(
        IGenericRepository<Exercise> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ExerciseDto>> Handle(
        GetExerciseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _repository
            .Get(exercise =>
                exercise.Id == request.Id &&
                !exercise.IsDeleted)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return Result<ExerciseDto>.NotFound("Exercise not found.");
        }

        return Result<ExerciseDto>.Success(response);
    }
}