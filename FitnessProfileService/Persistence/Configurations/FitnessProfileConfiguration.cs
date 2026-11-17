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

        }
    }
}
