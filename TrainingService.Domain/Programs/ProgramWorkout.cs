using TrainingService.Domain.Common;
using TrainingService.Domain.Workouts;

namespace TrainingService.Domain.Programs;

public class ProgramWorkout(Guid programId,Guid workoutId, int dayNumber, int weekNumber)
    : BaseEntity
{
    public Guid ProgramId { get; private set; } = programId;
    public virtual Program Program { get; private set; } = null!;
    public Guid WorkoutId { get; private set; } = workoutId;
    public virtual Workout Workout  {get; private set; } = null!;
    public int DayNumber { get; private set; } = dayNumber;
    public int WeekNumber { get; private set; } = weekNumber;
}