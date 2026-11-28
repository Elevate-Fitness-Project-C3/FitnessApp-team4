using TrainingService.Domain.Common;
using TrainingService.Domain.UserTrainingPrograms.Enums;

namespace TrainingService.Domain.UserTrainingPrograms;

public class UserTrainingProgram : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid ProgramId { get; set; }

    public UserTrainingProgramStatus Status { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? StoppedAt { get; set; }
}