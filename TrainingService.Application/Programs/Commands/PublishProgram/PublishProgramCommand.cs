using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Programs.Commands.PublishProgram;

public sealed record PublishProgramCommand(Guid ProgramId) : IRequest<Result<Guid>>;