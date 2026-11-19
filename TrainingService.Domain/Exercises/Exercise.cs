using TrainingService.Domain.Common;
using TrainingService.Domain.Exercises.Enums;

namespace TrainingService.Domain.Exercises;

public class Exercise : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public string TargetMuscleGroup { get; set; } = null!;

    public ExerciseTrackingType TrackingType { get; set; }

    public bool IsActive { get; set; } = true;
}