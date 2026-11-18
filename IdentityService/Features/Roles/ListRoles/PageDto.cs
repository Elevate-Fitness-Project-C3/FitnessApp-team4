namespace IdentityService.Features.Roles.ListRoles;

public sealed record PageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
