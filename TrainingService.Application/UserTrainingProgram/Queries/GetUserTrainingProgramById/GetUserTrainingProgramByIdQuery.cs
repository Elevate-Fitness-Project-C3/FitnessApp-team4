using MediatR;
using TrainingService.Application.UserTrainingProgram.Dtos;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramById;

public record GetUserTrainingProgramByIdQuery(Guid UserTrainingProgramId)
    : IRequest<UserTrainingProgramDto?>;