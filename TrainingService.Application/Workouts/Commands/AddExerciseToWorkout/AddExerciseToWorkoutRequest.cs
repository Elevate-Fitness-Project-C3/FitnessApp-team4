namespace TrainingService.Application.Workouts.Commands.AddExerciseToWorkout;

public record AddExerciseToWorkoutRequest(
    int Order,
    int? Sets,
    int? Reps,
    int? Duration
);