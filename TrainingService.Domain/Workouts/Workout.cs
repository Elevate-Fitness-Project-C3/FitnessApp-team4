using TrainingService.Domain.Common;
using TrainingService.Domain.Workouts.Enums;

namespace TrainingService.Domain.Workouts;

public class Workout : BaseEntity
{
    public string Title { get; set; } = null!;

    public WorkoutDifficulty Difficulty { get; set; }

    public int EstimatedDuration { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; }
        = new List<WorkoutExercise>();
}