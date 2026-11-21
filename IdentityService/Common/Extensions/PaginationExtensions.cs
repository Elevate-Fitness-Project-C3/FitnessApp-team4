namespace IdentityService.Common.Extensions;

public static class PaginationExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(checked((pageIndex - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResult<T>.Create(
            items,
            totalCount,
            pageIndex,
            pageSize);
    }
}
