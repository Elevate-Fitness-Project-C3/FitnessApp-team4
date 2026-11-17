using FitnessProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessProfileService.Persistence.Configurations
{
    public class FitnessProfileConfiguration : IEntityTypeConfiguration<FitnessProfile>
    {
        public void Configure(EntityTypeBuilder<FitnessProfile> builder)
        {

            builder.HasKey(fp => fp.Id);


            builder.Property(fp => fp.UserId)
                .IsRequired()
                .HasMaxLength(450);


            builder.HasIndex(fp => fp.UserId)
                .IsUnique();


            builder.HasQueryFilter(fp => !fp.IsDeleted);

            builder.Property(fp => fp.Gender)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();


            builder.Property(fp => fp.FitnessGoal)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(fp => fp.PhysicalActivityLevel)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(fp => fp.TrainingExperienceLevel)
                .HasConversion<string>()
                .HasMaxLength(100);
        }
    }
}
