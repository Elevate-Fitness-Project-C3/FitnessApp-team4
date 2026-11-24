using Ardalis.Result;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace TrainingService.Api.Common.Mapping;

public static class ResultExtensions
{
    public static IResult ToApiResponse<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            var response = ApiResponse<T>.Success(
                result.Value,
                string.IsNullOrWhiteSpace(result.SuccessMessage) ? null : result.SuccessMessage);

            var statusCode = result.Status == ResultStatus.Created
                ? StatusCodes.Status201Created
                : StatusCodes.Status200OK;

            return Results.Json(response, statusCode: statusCode);
        }

        return MapFailure<T>(
            result.Status,
            result.Errors.Concat(
                result.ValidationErrors.Select(x => x.ErrorMessage)));
    }

    public static IResult ToApiResponse(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Json(
                ApiResponse<object>.Success(null),
                statusCode: StatusCodes.Status200OK);
        }

        return MapFailure<object>(
            result.Status,
            result.Errors.Concat(
                result.ValidationErrors.Select(x => x.ErrorMessage)));
    }

    private static IResult MapFailure<T>(
        ResultStatus status,
        IEnumerable<string> errors)
    {
        var statusCode = status switch
        {
            ResultStatus.Invalid => StatusCodes.Status400BadRequest,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        var errorList = errors.ToList();

        var response = ApiResponse<T>.Failure(
            errorList.FirstOrDefault() ?? "An unexpected error occurred.",
            errorList);

        return Results.Json(response, statusCode: statusCode);
    }
}