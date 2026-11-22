using IdentityService.Common.Extensions;

namespace IdentityService.Features.Users.ListUsers;

internal sealed class ListUsersQueryHandler(AuthDbContext db)
    : IRequestHandler<ListUsersQuery, Result<PaginatedResult<UserDto>>>
{
    public async Task<Result<PaginatedResult<UserDto>>> Handle(ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(search) || x.LastName.Contains(search) ||
                (x.Email != null && x.Email.Contains(search)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (request.EmailConfirmed.HasValue)
        {
            query = query.Where(x => x.EmailConfirmed == request.EmailConfirmed.Value);
        }

        var orderedQuery = request.SortBy switch
        {
            UserSortBy.FirstName => query.ApplySorting(x => x.FirstName, request.SortDirection),

            UserSortBy.LastName => query.ApplySorting(x => x.LastName, request.SortDirection),

            UserSortBy.Email => query.ApplySorting(x => x.Email, request.SortDirection),

            UserSortBy.IsActive => query.ApplySorting(x => x.IsActive, request.SortDirection),

            UserSortBy.EmailConfirmed => query.ApplySorting(x => x.EmailConfirmed, request.SortDirection),

            UserSortBy.CreatedAt => query.ApplySorting(x => x.CreatedAt, request.SortDirection),

            _ => throw new ArgumentOutOfRangeException(nameof(request.SortBy))
        };

        return await orderedQuery.ThenBy(x => x.Id)
            .Select(x => new UserDto(x.Id, x.FirstName, x.LastName, x.Email, x.EmailConfirmed, x.PhoneNumber,
                x.IsActive, x.CreatedAt))
            .ToPaginatedResultAsync(request.PageIndex, request.PageSize, cancellationToken);
    }
}