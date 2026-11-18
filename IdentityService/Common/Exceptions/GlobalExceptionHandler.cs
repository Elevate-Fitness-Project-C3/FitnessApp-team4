using IdentityService.Common.Responses;
namespace IdentityService.Common.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => $"Validation.{group.Key}", group => group.Select(error => error.ErrorMessage).ToArray());
            var validationResponse = ApiResponse<object>.Fail(message: "Validation failed.", statusCode:
                                        StatusCodes.Status400BadRequest, errors: errors);
            httpContext.Response.StatusCode = validationResponse.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(validationResponse, cancellationToken);
            return true;
        }
        if (exception is Microsoft.Data.SqlClient.SqlException or DbUpdateException)
        {
            var conflict = exception is DbUpdateConcurrencyException;
            var sql = exception as Microsoft.Data.SqlClient.SqlException ?? exception.InnerException as Microsoft.Data.SqlClient.SqlException;
            logger.LogWarning("Persistence operation failed. TraceId={TraceId} ExceptionType={ExceptionType} SqlError={SqlError}",
                System.Diagnostics.Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
                exception.GetType().Name, sql?.Number);
            httpContext.Response.StatusCode = conflict ? 409 : 503;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(conflict ? "The resource changed. Reload before retrying." : "The database operation is temporarily unavailable.", httpContext.Response.StatusCode), cancellationToken);
            return true;
        }
        logger.LogError("Unhandled exception occurred. Type={ExceptionType} TraceId={TraceId}", exception.GetType().Name, httpContext.TraceIdentifier);
        var response = ApiResponse<object>.Fail(message: "An unexpected error occurred.", statusCode:
                                        StatusCodes.Status500InternalServerError);
        httpContext.Response.StatusCode = response.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
