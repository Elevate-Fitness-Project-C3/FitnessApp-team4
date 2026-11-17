using Microsoft.EntityFrameworkCore;
using TrainingService.Domain.Exercises;
using TrainingService.Domain.Workouts;

namespace TrainingService.Infrastructure.Persistence;

public class TrainingDbContext : DbContext
{
    public TrainingDbContext(DbContextOptions<TrainingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    
    public DbSet<Workout> Workouts => Set<Workout>();

    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TrainingDbContext).Assembly);
    }
}