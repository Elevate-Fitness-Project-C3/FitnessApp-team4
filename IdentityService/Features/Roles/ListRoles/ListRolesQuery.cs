namespace IdentityService.Features.Roles.ListRoles;

public sealed record ListRolesQuery(string? Search, int Page = 1, int PageSize = 25) : IRequest<Result<PageDto<RoleDto>>>;
