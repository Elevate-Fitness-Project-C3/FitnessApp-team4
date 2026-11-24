using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Commands.UpdateExercise;

public class UpdateExerciseCommandHandler
    : IRequestHandler<UpdateExerciseCommand, Result>
{
    private readonly IGenericRepository<Exercise> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExerciseCommandHandler(
        IGenericRepository<Exercise> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var exercise = await _repository.GetByIdAsync(request.Id);

        if (exercise is null || exercise.IsDeleted)
        {
            return Result.NotFound("Exercise not found.");
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

        return Result.Success();
    }
}