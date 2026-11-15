
namespace IdentityService.Features.Users.CheckEmailExists;

internal sealed record CheckEmailExistsQuery(string Email) : IRequest<Result<bool>>;
