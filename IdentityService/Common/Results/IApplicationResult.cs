namespace IdentityService.Common.Results;

public interface IApplicationResult
{
    bool IsSuccess { get; }

    IReadOnlyList<Error> Errors { get; }
}

public interface IResult<out TValue> : IApplicationResult
{
    TValue Value { get; }
}