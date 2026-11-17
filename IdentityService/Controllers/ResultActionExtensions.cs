using IdentityService.Common.Responses;
using Microsoft.AspNetCore.Mvc;
namespace IdentityService.Controllers;

public static class ResultActionExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        var response = result.ToApiResponse(successStatusCode);
        return new ObjectResult(response) { StatusCode = response.StatusCode };
    }
}
