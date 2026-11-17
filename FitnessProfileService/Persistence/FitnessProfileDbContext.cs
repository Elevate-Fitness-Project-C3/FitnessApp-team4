using FitnessProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessProfileService.Persistence
{
    public class FitnessProfileDbContext : DbContext
    {
        public FitnessProfileDbContext(DbContextOptions<FitnessProfileDbContext> options)
              : base(options)
        {
        }

        public DbSet<FitnessProfile> FitnessProfiles { get; set; }

        public DbSet<BodyMeasurement> BodyMeasurements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FitnessProfileDbContext).Assembly);
        }
    }
}
