using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingService.Domain.Programs;
using TrainingService.Domain.UserTrainingPrograms;

namespace TrainingService.Infrastructure.Configurations;

public class UserTrainingProgramConfiguration
    : IEntityTypeConfiguration<UserTrainingProgram>
{
    public void Configure(EntityTypeBuilder<UserTrainingProgram> builder)
    {
        builder.ToTable("UserTrainingPrograms");

        builder.HasKey(utp => utp.Id);

        builder.Property(utp => utp.UserId)
            .IsRequired();

        builder.Property(utp => utp.ProgramId)
            .IsRequired();

        builder.Property(utp => utp.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(utp => utp.StartedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(utp => utp.CompletedAt)
            .HasColumnType("datetime2");

        builder.Property(utp => utp.StoppedAt)
            .HasColumnType("datetime2");

        builder.Property(utp => utp.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(utp => utp.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(utp => utp.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(utp => utp.DeletedAt)
            .HasColumnType("datetime2");

        builder.HasOne<Program>()
            .WithMany()
            .HasForeignKey(utp => utp.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(utp => utp.UserId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");
    }
}