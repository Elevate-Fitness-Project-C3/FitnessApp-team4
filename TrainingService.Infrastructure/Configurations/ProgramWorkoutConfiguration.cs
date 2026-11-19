using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingService.Domain.Programs;

namespace TrainingService.Infrastructure.Configurations;

public class ProgramWorkoutConfiguration: IEntityTypeConfiguration<ProgramWorkout>
{
    public void Configure(EntityTypeBuilder<ProgramWorkout> builder)
    {
        builder.HasIndex(x => new
            {
                x.ProgramId,
                x.WeekNumber,
                x.DayNumber
            })
            .IsUnique()
            .HasDatabaseName("IX_ProgramWorkout_Program_Week_Day");
        builder.HasOne(x => x.Workout)
            .WithMany()
            .HasForeignKey(x => x.WorkoutId);
        builder.HasOne(x => x.Program)
            .WithMany(p=>p.WorkoutSlots)
            .HasForeignKey(x => x.ProgramId);


    }
}