using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Commands.CreateExercise;

public class CreateExerciseCommandHandler
    : IRequestHandler<CreateExerciseCommand, Result<ExerciseDto>>
{
    private readonly IGenericRepository<Exercise> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExerciseCommandHandler(
        IGenericRepository<Exercise> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ExerciseDto>> Handle(
        CreateExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Instructions = request.Instructions.Trim(),
            TargetMuscleGroup = request.TargetMuscleGroup.Trim(),
            TrackingType = request.TrackingType,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(exercise);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new ExerciseDto(
            exercise.Id,
            exercise.Name,
            exercise.Description,
            exercise.Instructions,
            exercise.TargetMuscleGroup,
            exercise.TrackingType,
            exercise.IsActive,
            exercise.CreatedAt,
            exercise.UpdatedAt
        );

        return Result<ExerciseDto>.Created(response);
    }
}