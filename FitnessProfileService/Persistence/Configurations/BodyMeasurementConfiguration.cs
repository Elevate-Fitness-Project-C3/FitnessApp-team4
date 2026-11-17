using FitnessProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessProfileService.Persistence.Configurations
{
    public class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
    {
        public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
        {

            builder.HasKey(bm => bm.Id);


            builder.Property(bm => bm.HeightCm)
                .HasPrecision(5,2)
                .IsRequired();


            builder.Property(bm => bm.WeightKg)
                .HasPrecision(5,2)
                .IsRequired();


            builder.Property(bm => bm.RecordedAt)
                .IsRequired();


            builder.HasOne(bm => bm.FitnessProfile)
                   .WithMany(fp => fp.BodyMeasurements)
                   .HasForeignKey(bm => bm.FitnessProfileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(bm => !bm.IsDeleted && !bm.FitnessProfile.IsDeleted);

        }
    }
}
