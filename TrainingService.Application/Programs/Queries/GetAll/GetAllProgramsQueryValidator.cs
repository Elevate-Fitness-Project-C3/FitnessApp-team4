using FluentValidation;

namespace TrainingService.Application.Programs.Queries.GetAll;

public sealed class GetAllProgramsQueryValidator : AbstractValidator<GetAllProgramsQuery>
{
    public GetAllProgramsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
