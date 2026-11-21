using Ardalis.Result;
using MediatR;
using TrainingService.Application.Exercises.Dtos;
using TrainingService.Domain.Exercises.Enums;

namespace TrainingService.Application.Exercises.Commands.UpdateExercise;

public record UpdateExerciseCommand(
    Guid Id,
    string Name,
    string? Description,
    string Instructions,
    string TargetMuscleGroup,
    ExerciseTrackingType TrackingType,
    bool IsActive
) : IRequest<Result<ExerciseDto>>;