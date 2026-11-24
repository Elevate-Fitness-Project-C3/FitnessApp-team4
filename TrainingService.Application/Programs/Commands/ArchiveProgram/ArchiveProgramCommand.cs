using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Programs.Commands.ArchiveProgram;

public sealed record ArchiveProgramCommand(Guid ProgramId) : IRequest<Result<Guid>>;
