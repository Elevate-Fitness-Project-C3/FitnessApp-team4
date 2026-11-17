namespace FitnessProfileService.Domain.Entities
{
    public class BodyMeasurement : BaseEntity
    {
        public Guid FitnessProfileId { get; set; } // Foreign Key

        public decimal HeightCm { get; set; }
        
        public decimal WeightKg { get; set; }

        public DateTimeOffset RecordedAt { get; set; }

        public FitnessProfile FitnessProfile { get; set; } = null!;
    }
}
