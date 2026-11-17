using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingService.Domain.Exercises;

namespace TrainingService.Infrastructure.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Instructions)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.TargetMuscleGroup)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.TrackingType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("datetime2");
        
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnType("datetime2");
    }
}