using TrainingService.Application.Common.Interfaces;

namespace TrainingService.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor)
    : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var id)
                ? id
                : throw new UnauthorizedAccessException(
                    "Invalid or missing user id.");
        }
    }
}