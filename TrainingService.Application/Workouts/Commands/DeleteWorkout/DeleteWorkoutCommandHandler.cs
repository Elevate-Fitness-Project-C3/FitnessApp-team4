using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Commands.DeleteWorkout;

public class DeleteWorkoutCommandHandler
    : IRequestHandler<DeleteWorkoutCommand, Result>
{
    private readonly IGenericRepository<Workout> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkoutCommandHandler(
        IGenericRepository<Workout> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteWorkoutCommand request,
        CancellationToken cancellationToken)
    {
        var workout = await _repository.GetByIdAsync(request.Id);

        if (workout is null || workout.IsDeleted)
        {
            return Result.NotFound("Workout not found.");
        }

        workout.IsDeleted = true;
        workout.DeletedAt = DateTime.UtcNow;
        workout.UpdatedAt = DateTime.UtcNow;

        _repository.Update(workout);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}