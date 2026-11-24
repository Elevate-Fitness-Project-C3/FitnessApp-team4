using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrainingService.Application.Common.Interfaces;
using TrainingService.Domain.Workouts;

namespace TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;

public class AddExerciseToWorkoutCommandHandler
    : IRequestHandler<AddExerciseToWorkoutCommand, Result<Guid>>
{
    private readonly IGenericRepository<WorkoutExercise> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddExerciseToWorkoutCommandHandler(
        IGenericRepository<WorkoutExercise> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        AddExerciseToWorkoutCommand request,
        CancellationToken cancellationToken)
    {
        var orderExists = await _repository
            .Get(we =>
                we.WorkoutId == request.WorkoutId &&
                we.Order == request.Order)
            .AnyAsync(cancellationToken);

        if (orderExists)
        {
            return Result<Guid>.Invalid(
                new ValidationError("This order is already used in the workout."));
        }

        var workoutExercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = request.WorkoutId,
            ExerciseId = request.ExerciseId,
            Order = request.Order,
            Sets = request.Sets,
            Reps = request.Reps,
            Duration = request.Duration,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(workoutExercise);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Created(workoutExercise.Id);
    }
}