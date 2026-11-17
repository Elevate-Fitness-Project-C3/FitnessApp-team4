namespace IdentityService.Common.Results;

public static class Result
{
    public static Success Success => default;
}
public sealed class Result<TValue> : IFailable<Result<TValue>>
{
    private readonly TValue? _value;
    private readonly IReadOnlyList<Error> _errors;
    private Result(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        _errors = [];
        IsSuccess = true;
    }
    private Result(Error error) : this(new[] { error })
    {
    }
    private Result(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.ToList();
        if (errorList.Count == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }
        if (errorList.Any(error => !error.IsValid))
        {
            throw new ArgumentException("All errors must be valid.", nameof(errors));
        }
        _errors = errorList.AsReadOnly();
        IsSuccess = false;
    }
    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors => _errors;
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
    public Error TopError => !IsSuccess
        ? _errors[0]
        : throw new InvalidOperationException("Cannot access errors from a successful result.");
    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> FromErrors(IEnumerable<Error> errors) => Failure(errors);
    public static Result<TValue> Failure(IEnumerable<Error> errors) => new(errors);
    public static implicit operator Result<TValue>(TValue value) => new(value);
    public static implicit operator Result<TValue>(Error error) => new(error);
}
public readonly record struct Success;
