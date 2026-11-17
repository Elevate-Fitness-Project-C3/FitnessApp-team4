using TrainingService.Domain.Common;

namespace TrainingService.Domain.Programs;

public class ProgramWorkout(Guid programId,Guid workoutId, int dayNumber, int weekNumber)
    : BaseEntity
{
    public Guid ProgramId { get; private set; } = programId;
    public Guid WorkoutId { get; private set; } = workoutId;
    public int DayNumber { get; private set; } = dayNumber;
    public int WeekNumber { get; private set; } = weekNumber;
}