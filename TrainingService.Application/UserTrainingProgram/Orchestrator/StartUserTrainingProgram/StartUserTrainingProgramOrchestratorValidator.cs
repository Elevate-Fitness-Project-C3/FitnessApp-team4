using FluentValidation;

namespace TrainingService.Application.UserTrainingProgram.Orchestrator.StartUserTrainingProgram;

public class StartUserTrainingProgramOrchestratorValidator
    : AbstractValidator<StartUserTrainingProgramOrchestrator>
{
    public StartUserTrainingProgramOrchestratorValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty()
            .WithMessage("ProgramId is required.");
    }
}