using Ardalis.Result;
using MediatR;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises.Enums;

namespace TrainingService.Application.Exercises.Commands.CreateExercise;

public record CreateExerciseCommand(
    string Name,
    string? Description,
    string Instructions,
    string TargetMuscleGroup,
    ExerciseTrackingType TrackingType,
    bool IsActive = true
) : IRequest<Result<ExerciseDto>>;