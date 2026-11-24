using Ardalis.Result;
using MediatR;

namespace TrainingService.Application.Exercises.Commands.DeleteExercise;


public record DeleteExerciseCommand(Guid Id) : IRequest<Result>;