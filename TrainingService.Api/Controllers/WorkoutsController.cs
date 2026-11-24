using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;
using TrainingService.Application.Workouts.Commands.CreateWorkout;
using TrainingService.Application.Workouts.Commands.DeleteWorkout;
using TrainingService.Application.Workouts.Commands.UpdateWorkout;
using TrainingService.Application.Workouts.Queries.GetAll;
using TrainingService.Application.Workouts.Queries.GetById;

namespace TrainingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkoutsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkoutsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IResult> Create(
        [FromBody] CreateWorkoutCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToApiResponse();
    }
    
    [HttpPost("{workoutId:guid}/Exercises/{exerciseId:guid}")]
    public async Task<IResult> AddExercise(
        Guid workoutId,
        Guid exerciseId,
        [FromBody] AddExerciseToWorkoutRequest request)
    {
        var orchestrator = new AddExerciseToWorkoutOrchestrator(
            workoutId,
            exerciseId,
            request.Order,
            request.Sets,
            request.Reps,
            request.Duration
        );

        var result = await _mediator.Send(orchestrator);

        return result.ToApiResponse();
    }

    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateWorkoutRequest request)
    {
        var command = new UpdateWorkoutCommand(
            id,
            request.Title,
            request.Difficulty,
            request.EstimatedDuration
        );

        var result = await _mediator.Send(command);

        return result.ToApiResponse();
    }

    [HttpGet]
    public async Task<IResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllWorkoutsQuery());
        return result.ToApiResponse();
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkoutByIdQuery(id));
        return result.ToApiResponse();
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteWorkoutCommand(id));
        return result.ToApiResponse();
    }
}