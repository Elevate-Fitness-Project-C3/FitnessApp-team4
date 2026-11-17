using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingService.Domain.Workouts;

namespace TrainingService.Infrastructure.Configurations;

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("WorkoutExercises");

        builder.HasKey(we => we.Id);

        builder.HasOne(we => we.Exercise)
            .WithMany()
            .HasForeignKey(we => we.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(we => we.Order)
            .IsRequired();

        builder.HasIndex(we => new { we.WorkoutId, we.Order })
            .IsUnique();

        builder.Property(we => we.Sets);

        builder.Property(we => we.Reps);

        builder.Property(we => we.Duration);

        builder.Property(we => we.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(we => we.UpdatedAt)
            .HasColumnType("datetime2");
        
        builder.Property(we => we.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(we => we.DeletedAt)
            .HasColumnType("datetime2");
    }
}