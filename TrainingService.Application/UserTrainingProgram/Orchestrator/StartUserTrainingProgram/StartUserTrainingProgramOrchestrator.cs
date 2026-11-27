using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.UserTrainingProgram.Orchestrator.StartUserTrainingProgram;

public record StartUserTrainingProgramOrchestrator(Guid ProgramId)
    : IRequest<Result<Guid>>;