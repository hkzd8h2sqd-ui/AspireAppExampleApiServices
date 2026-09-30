using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Web;

/// <summary>
/// Query helpers that keep state-store queries portable between SQL Server and SQLite.
/// </summary>
internal static class QueryableExtensions
{
    /// <summary>
    /// Materializes the query and orders the result in memory.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TKey">Sort key type.</typeparam>
    /// <param name="query">The filtered query to execute.</param>
    /// <param name="keySelector">Sort key, evaluated on the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query result ordered ascending by <paramref name="keySelector"/>.</returns>
    /// <remarks>
    /// SQLite cannot translate <see cref="DateTimeOffset"/> in <c>ORDER BY</c>, so sorting on such
    /// columns must happen after the (already filtered and bounded) result is loaded.
    /// </remarks>
    public static async Task<List<T>> ToListOrderedByAsync<T, TKey>(
        this IQueryable<T> query,
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default)
    {
        var items = await query.ToListAsync(cancellationToken);
        return [.. items.OrderBy(keySelector)];
    }
}
