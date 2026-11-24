using Ardalis.Result;
using MediatR;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Commands.CreateWorkout;

public class CreateWorkoutCommandHandler
    : IRequestHandler<CreateWorkoutCommand, Result<Guid>>
{
    private readonly IGenericRepository<Workout> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkoutCommandHandler(
        IGenericRepository<Workout> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateWorkoutCommand request,
        CancellationToken cancellationToken)
    {
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Difficulty = request.Difficulty,
            EstimatedDuration = request.EstimatedDuration,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(workout);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(workout.Id);
    }
}