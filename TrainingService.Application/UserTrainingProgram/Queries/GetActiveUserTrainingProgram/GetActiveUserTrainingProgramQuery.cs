using MediatR;
using TrainingService.Application.UserTrainingProgram.Dtos;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetActiveUserTrainingProgram;

public record GetActiveUserTrainingProgramQuery
    : IRequest<UserTrainingProgramDto?>;