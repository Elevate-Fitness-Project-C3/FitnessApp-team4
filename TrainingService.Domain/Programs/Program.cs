using TrainingService.Domain.Common;
using TrainingService.Domain.Programs.Enums;

namespace TrainingService.Domain.Programs;

public class Program :BaseEntity
{
    public ProgramGoal Goal { get; private set; }
    public ProgramLevel Level { get; private set; }

    public int DurationWeeks { get; private set; }
    public int DaysPerWeek { get; private set; }

    public AccessTier AccessTier { get; private set; }
    public string Description { get; private set; } = null!;

    public ProgramStatus Status { get; private set; }
    
    public ICollection<ProgramWorkout> WorkoutSlots { get; private set; }=new List<ProgramWorkout>();
    
    public Program(ProgramGoal goal, ProgramLevel level, int durationWeeks, int daysPerWeek, AccessTier accessTier, string description)
    {
        Goal = goal;
        Level = level;
        DurationWeeks = durationWeeks;
        DaysPerWeek = daysPerWeek;
        AccessTier = accessTier;
        Description = description;
        ProgramStatus status = ProgramStatus.Draft;
    }
    public void AddWorkoutSlot(
        Guid workoutId,
        int weekNumber,
        int dayNumber)
    {
        if (weekNumber < 1 || weekNumber > DurationWeeks)
            throw new Exception(
                "Week number must be between 1 and DurationWeeks.");

        if (dayNumber < 1 || dayNumber > DaysPerWeek)
            throw new Exception(
                "Day number must be between 1 and DaysPerWeek.");


        var slotExists = WorkoutSlots.Any(x =>
            x.WeekNumber == weekNumber &&
            x.DayNumber == dayNumber);

        if (slotExists)
            throw new Exception(
                "A workout is already assigned to this slot.");
        
        WorkoutSlots.Add(
            new ProgramWorkout(
                Id,
                workoutId,
                dayNumber,
                weekNumber));
    }

    public void Publish()
    {
        if (Status == ProgramStatus.Published)
            throw new InvalidOperationException("Program is already published.");

        Status = ProgramStatus.Published;
    }

    public void Archive()
    {
        if (Status == ProgramStatus.Archived)
            throw new InvalidOperationException("Program is already archived.");

        Status = ProgramStatus.Archived;
    }

    
}