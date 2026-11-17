using FitnessProfileService.Domain.Enums;

namespace FitnessProfileService.Domain.Entities;

public class FitnessProfile : BaseEntity
{
    public string UserId { get; set; } = null!; // comes from the authentication service, unique identifier for the user

    public Gender Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public FitnessGoal FitnessGoal { get; set; }

    public PhysicalActivityLevel PhysicalActivityLevel { get; set; }

    public TrainingExperienceLevel? TrainingExperienceLevel { get; set; }

    public int? AvailableDaysPerWeek { get; set; }

    public bool IsOnboardingComplete { get; set; }

    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();

}
