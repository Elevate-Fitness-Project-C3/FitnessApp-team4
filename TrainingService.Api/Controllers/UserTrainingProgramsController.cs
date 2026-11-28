using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.UserTrainingProgram.Commands.StopUserTrainingProgram;
using TrainingService.Application.UserTrainingProgram.Orchestrator.StartUserTrainingProgram;
using TrainingService.Application.UserTrainingProgram.Queries.GetActiveUserTrainingProgram;
using TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramById;
using TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramHistory;

namespace TrainingService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/user-training-programs")]
public class UserTrainingProgramsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserTrainingProgramsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start")]
    public async Task<IResult> Start(
        [FromBody] StartUserTrainingProgramRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new StartUserTrainingProgramOrchestrator(request.ProgramId),
            cancellationToken);

        return result.ToApiResponse();
    }
    
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(
        CancellationToken cancellationToken)
    {
        var activeProgram = await _mediator.Send(
            new GetActiveUserTrainingProgramQuery(),
            cancellationToken);

        if (activeProgram is null)
            return NotFound();

        return Ok(activeProgram);
    }
    
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        CancellationToken cancellationToken)
    {
        var history = await _mediator.Send(
            new GetUserTrainingProgramHistoryQuery(),
            cancellationToken);

        return Ok(history);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userTrainingProgram = await _mediator.Send(
            new GetUserTrainingProgramByIdQuery(id),
            cancellationToken);

        if (userTrainingProgram is null)
            return NotFound();

        return Ok(userTrainingProgram);
    }
    
    [HttpPatch("stop")]
    public async Task<IResult> Stop(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new StopUserTrainingProgramCommand(),
            cancellationToken);

        return result.ToApiResponse();
    }
}