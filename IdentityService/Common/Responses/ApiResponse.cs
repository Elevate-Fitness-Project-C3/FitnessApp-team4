namespace IdentityService.Common.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = StatusCodes.Status200OK)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (statusCode is < 200 or >= 300 or StatusCodes.Status204NoContent or StatusCodes.Status205ResetContent)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "A successful response must use a success status code that allows a response body.");
        }
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }
    public static ApiResponse<T> Created(T data, string message = "Created successfully")
    {
        return Ok(data, message, StatusCodes.Status201Created);
    }
    public static ApiResponse<T> Fail(string message, int statusCode = StatusCodes.Status400BadRequest, IDictionary<string, string[]>? errors = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (statusCode is < 400 or >= 600)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "A failed response must use a client or server error status code.");
        }
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };
    }
}
