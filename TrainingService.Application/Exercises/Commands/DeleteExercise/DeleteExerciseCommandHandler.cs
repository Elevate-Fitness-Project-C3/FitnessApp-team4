using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Exercises;

namespace TrainingService.Application.Exercises.Commands.DeleteExercise;

public class DeleteExerciseCommandHandler
    : IRequestHandler<DeleteExerciseCommand, Result>
{
    private readonly IGenericRepository<Exercise> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteExerciseCommandHandler(
        IGenericRepository<Exercise> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteExerciseCommand request,
        CancellationToken cancellationToken)
    {
        var exercise = await _repository.GetByIdAsync(request.Id);

        if (exercise is null || exercise.IsDeleted)
        {
            return Result.NotFound("Exercise not found.");
        }

        exercise.IsDeleted = true;
        exercise.IsActive = false;
        exercise.DeletedAt = DateTime.UtcNow;
        exercise.UpdatedAt = DateTime.UtcNow;

        _repository.Update(exercise);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}