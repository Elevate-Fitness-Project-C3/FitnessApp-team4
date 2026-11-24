using Ardalis.Result;
using MediatR;
using TrainingService.Application.Workouts.Queries.GetById;
using TrainingService.Application.Exercises.Queries.GetById;

namespace TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;

public class AddExerciseToWorkoutOrchestratorHandler
    : IRequestHandler<AddExerciseToWorkoutOrchestrator, Result<Guid>>
{
    private readonly IMediator _mediator;

    public AddExerciseToWorkoutOrchestratorHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(
        AddExerciseToWorkoutOrchestrator request,
        CancellationToken cancellationToken)
    {
        var workoutResult = await _mediator.Send(
            new GetWorkoutByIdQuery(request.WorkoutId),
            cancellationToken);

        if (!workoutResult.IsSuccess)
        {
            return Result<Guid>.NotFound("Workout not found.");
        }
        
        var exerciseResult = await _mediator.Send(
            new GetExerciseByIdQuery(request.ExerciseId),
            cancellationToken);

        if (!exerciseResult.IsSuccess)
        {
            return Result<Guid>.NotFound("Exercise not found.");
        }

        if (!exerciseResult.Value.IsActive)
        {
            return Result<Guid>.Invalid(
                new ValidationError("Inactive exercises cannot be added to workouts."));
        }

        var command = new AddExerciseToWorkoutCommand(
            request.WorkoutId,
            request.ExerciseId,
            request.Order,
            request.Sets,
            request.Reps,
            request.Duration);

        return await _mediator.Send(command, cancellationToken);
    }
}