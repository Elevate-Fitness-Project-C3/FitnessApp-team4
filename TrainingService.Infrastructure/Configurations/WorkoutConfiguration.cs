using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingService.Domain.Workouts;

namespace TrainingService.Infrastructure.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.ToTable("Workouts");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.Difficulty)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(w => w.EstimatedDuration)
            .IsRequired();

        builder.HasMany(w => w.WorkoutExercises)
            .WithOne(we => we.Workout)
            .HasForeignKey(we => we.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(w => w.UpdatedAt)
            .HasColumnType("datetime2");
        
        builder.Property(w => w.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.DeletedAt)
            .HasColumnType("datetime2");
    }
}