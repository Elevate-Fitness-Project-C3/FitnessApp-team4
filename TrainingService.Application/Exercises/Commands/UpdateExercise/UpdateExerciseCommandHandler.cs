using Ardalis.Result;
using FluentValidation;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Commands.UpdateExercise;

public class UpdateExerciseCommandHandler
    : IRequestHandler<UpdateExerciseCommand, Result<ExerciseDto>>
{
    private readonly IGenericRepository<Exercise> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateExerciseCommand> _validator;

    public UpdateExerciseCommandHandler(
        IGenericRepository<Exercise> repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateExerciseCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<ExerciseDto>> Handle(
        UpdateExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result<ExerciseDto>.Invalid(
                validationResult.Errors
                    .Select(error => new ValidationError(
                        error.PropertyName,
                        error.ErrorMessage))
                    .ToArray());
        }

        var exercise = await _repository.GetByIdAsync(request.Id);

        if (exercise is null || exercise.IsDeleted)
        {
            return Result<ExerciseDto>.NotFound("Exercise not found.");
        }

        exercise.Name = request.Name.Trim();
        exercise.Description = request.Description?.Trim();
        exercise.Instructions = request.Instructions.Trim();
        exercise.TargetMuscleGroup = request.TargetMuscleGroup.Trim();
        exercise.TrackingType = request.TrackingType;
        exercise.IsActive = request.IsActive;
        exercise.UpdatedAt = DateTime.UtcNow;

        _repository.Update(exercise);

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

        return Result<ExerciseDto>.Success(response);
    }
}
