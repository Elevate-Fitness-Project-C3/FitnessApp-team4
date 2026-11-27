using MediatR;
using TrainingService.Domain.Programs;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetProgramById;

public record GetProgramByIdQuery(Guid ProgramId)
    : IRequest<Program?>;