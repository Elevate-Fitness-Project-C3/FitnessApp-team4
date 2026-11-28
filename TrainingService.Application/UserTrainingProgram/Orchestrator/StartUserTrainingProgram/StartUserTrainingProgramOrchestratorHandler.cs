using Ardalis.Result;
using MediatR;
using TrainingService.Application.UserTrainingProgram.Queries.GetProgramById;
using TrainingService.Application.UserTrainingProgram.Queries.GetActiveUserTrainingProgram;
using TrainingService.Application.UserTrainingProgram.Commands.CreateUserTrainingProgram;
using TrainingService.Application.UserTrainingProgram.Orchestrator.StartUserTrainingProgram;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Application.UserTrainingProgram.Orchestrators.StartUserTrainingProgram;

public class StartUserTrainingProgramOrchestratorHandler
    : IRequestHandler<StartUserTrainingProgramOrchestrator, Result<Guid>>
{
    private readonly IMediator _mediator;

    public StartUserTrainingProgramOrchestratorHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(
        StartUserTrainingProgramOrchestrator request,
        CancellationToken cancellationToken)
    {
        var program = await _mediator.Send(
            new GetProgramByIdQuery(request.ProgramId),
            cancellationToken);

        if (program is null)
            return Result<Guid>.NotFound("Program not found.");

        if (program.Status != ProgramStatus.Published)
            return Result<Guid>.Conflict("Only published programs can be started.");

        var activeProgram = await _mediator.Send(
            new GetActiveUserTrainingProgramQuery(),
            cancellationToken);

        if (activeProgram is not null)
        {
            if (activeProgram.ProgramId == request.ProgramId)
                return Result<Guid>.Success(activeProgram.Id);

            return Result<Guid>.Conflict(
                "You already have an active training program.");
        }

        return await _mediator.Send(
            new CreateUserTrainingProgramCommand(request.ProgramId),
            cancellationToken);
    }
}