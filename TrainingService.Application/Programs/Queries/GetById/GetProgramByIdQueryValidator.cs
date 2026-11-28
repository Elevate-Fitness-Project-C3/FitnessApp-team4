using FluentValidation;

namespace TrainingService.Application.Programs.Queries.GetById;

public sealed class GetProgramByIdQueryValidator : AbstractValidator<GetProgramByIdQuery>
{
    public GetProgramByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Program ID is required.");
    }
}
