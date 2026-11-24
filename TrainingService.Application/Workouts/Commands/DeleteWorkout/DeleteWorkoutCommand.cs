using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Workouts.Commands.DeleteWorkout;

public record DeleteWorkoutCommand(Guid Id) : IRequest<Result>;