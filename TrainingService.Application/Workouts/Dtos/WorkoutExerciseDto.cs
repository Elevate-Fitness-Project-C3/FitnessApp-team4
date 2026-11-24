namespace TrainingService.Application.Workouts.Dtos;

public record WorkoutExerciseDto(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    int Order,
    int? Sets,
    int? Reps,
    int? Duration
);