using MediatR;
using TrainingService.Application.UserTrainingProgram.Dtos;

namespace TrainingService.Application.UserTrainingProgram.Queries.GetUserTrainingProgramHistory;

public record GetUserTrainingProgramHistoryQuery
    : IRequest<IReadOnlyList<UserTrainingProgramDto>>;