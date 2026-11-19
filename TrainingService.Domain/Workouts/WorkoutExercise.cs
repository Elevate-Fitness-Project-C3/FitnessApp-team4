using TrainingService.Domain.Common;
using TrainingService.Domain.Exercises;

namespace TrainingService.Domain.Workouts;

public class WorkoutExercise : BaseEntity
{
    public Guid WorkoutId { get; set; }

    public Workout Workout { get; set; } = null!;

    public Guid ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }

    public int? Sets { get; set; }

    public int? Reps { get; set; }

    public int? Duration { get; set; }
}