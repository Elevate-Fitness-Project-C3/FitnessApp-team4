
using System.Linq.Expressions;
using IdentityService.Common.Sorting;

namespace IdentityService.Common.Extensions;

public static class SortingExtensions
{
    public static IOrderedQueryable<T> ApplySorting<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);

        return direction switch
        {
            SortDirection.Asc => query.OrderBy(keySelector),
            SortDirection.Desc => query.OrderByDescending(keySelector),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    public static IOrderedQueryable<T> ThenSortBy<T, TKey>(
        this IOrderedQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        SortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keySelector);

        return direction switch
        {
            SortDirection.Asc => query.ThenBy(keySelector),
            SortDirection.Desc => query.ThenByDescending(keySelector),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }
}
