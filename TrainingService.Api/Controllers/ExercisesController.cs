using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.Exercises.Commands.CreateExercise;
using TrainingService.Application.Exercises.Commands.DeleteExercise;
using TrainingService.Application.Exercises.Commands.UpdateExercise;
using TrainingService.Application.Exercises.Queries.GetAll;
using TrainingService.Application.Exercises.Queries.GetById;

namespace TrainingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExercisesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IResult> CreateExercise(
        [FromBody] CreateExerciseCommand command)
    {
        var result = await _mediator.Send(command);

        return result.ToApiResponse();
    }

    [HttpGet]
    public async Task<IResult> GetAllExercises()
    {
        var result = await _mediator.Send(new GetExercisesQuery());

        return result.ToApiResponse();
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IResult> GetExerciseById(Guid id)
    {
        var query = new GetExerciseByIdQuery(id);

        var result = await _mediator.Send(query);

        return result.ToApiResponse();
    }

    [HttpPut("{id:guid}")]
    public async Task<IResult> UpdateExercise(
        Guid id,
        [FromBody] UpdateExerciseCommand command)
    {
        var result = await _mediator.Send(command with { Id = id });

        return result.ToApiResponse();
    }

    
    [HttpDelete("{id:guid}")]
    public async Task<IResult> DeleteExercise(Guid id)
    {
        var command = new DeleteExerciseCommand(id);

        var result = await _mediator.Send(command);

        return result.ToApiResponse();
    }
}