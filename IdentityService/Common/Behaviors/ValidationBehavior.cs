
namespace IdentityService.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull where TResponse : IFailable<TResponse>
{
    private readonly IReadOnlyList<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators.ToArray();
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_validators.Count == 0)
            return await next();
        var failures = new List<ValidationFailure>();
        // Execute sequentially to support shared scoped dependencies.
        foreach (var validator in _validators)
        {
            var context = new ValidationContext<TRequest>(request);
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }
        if (failures.Count > 0)
            return TResponse.FromErrors(failures.Select(x => Error.Validation($"Validation.{x.PropertyName}", x.ErrorMessage)));
        return await next();
    }
}
