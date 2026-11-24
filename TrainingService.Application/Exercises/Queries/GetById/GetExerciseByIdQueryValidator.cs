using FluentValidation;

namespace TrainingService.Application.Exercises.Queries.GetById;

public class GetExerciseByIdQueryValidator
    : AbstractValidator<GetExerciseByIdQuery>
{
    public GetExerciseByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Exercise ID is required.");
    }
}