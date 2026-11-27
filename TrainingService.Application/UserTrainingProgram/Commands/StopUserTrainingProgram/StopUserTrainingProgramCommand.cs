using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.UserTrainingProgram.Commands.StopUserTrainingProgram;

public record StopUserTrainingProgramCommand : IRequest<Result>;