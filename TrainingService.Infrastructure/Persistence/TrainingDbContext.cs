using Microsoft.EntityFrameworkCore;
using TrainingService.Domain.Exercises;
using TrainingService.Domain.Workouts;
using TrainingService.Domain.Programs;
using TrainingService.Domain.UserTrainingPrograms;

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
    public DbSet<Program>  Programs { get; set; }
    public DbSet<ProgramWorkout>  ProgramWorkouts { get; set; }
    public DbSet<UserTrainingProgram> UserTrainingPrograms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TrainingDbContext).Assembly);
    }
}