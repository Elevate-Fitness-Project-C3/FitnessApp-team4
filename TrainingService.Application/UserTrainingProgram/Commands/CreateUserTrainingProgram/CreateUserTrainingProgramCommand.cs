using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.UserTrainingProgram.Commands.CreateUserTrainingProgram;

public record CreateUserTrainingProgramCommand(Guid ProgramId)
    : IRequest<Result<Guid>>;