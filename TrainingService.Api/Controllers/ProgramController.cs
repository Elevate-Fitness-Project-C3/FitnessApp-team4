using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrainingService.Api.Common.Mapping;
using TrainingService.Application.Programs.Commands.CreateProgram;

namespace TrainingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgramController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> CreateProgram([FromBody] CreateProgramCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return result.ToApiResponse();
    }
    
}