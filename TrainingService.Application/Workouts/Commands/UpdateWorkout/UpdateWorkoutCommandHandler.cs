using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Commands.UpdateWorkout;

public class UpdateWorkoutCommandHandler
    : IRequestHandler<UpdateWorkoutCommand, Result>
{
    private readonly IGenericRepository<Workout> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkoutCommandHandler(
        IGenericRepository<Workout> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateWorkoutCommand request,
        CancellationToken cancellationToken)
    {
        var workout = await _repository.GetByIdAsync(request.Id);

        if (workout is null || workout.IsDeleted)
        {
            return Result.NotFound("Workout not found.");
        }

        workout.Title = request.Title.Trim();
        workout.Difficulty = request.Difficulty;
        workout.EstimatedDuration = request.EstimatedDuration;
        workout.UpdatedAt = DateTime.UtcNow;

        _repository.Update(workout);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}