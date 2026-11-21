
using IdentityService.Common.Sorting;

namespace IdentityService.Features.Users.ListUsers;

public sealed record ListUsersQuery(
    int PageIndex = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    bool? EmailConfirmed = null,
    UserSortBy SortBy = UserSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Desc
) : IRequest<PaginatedResult<UserDto>>;
