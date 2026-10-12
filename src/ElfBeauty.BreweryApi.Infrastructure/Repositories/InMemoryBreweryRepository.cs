using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;

namespace ElfBeauty.BreweryApi.Infrastructure.Repositories;

/// Default in-memory storage implementation (thread-safe) satisfying the base storage requirement.
/// Does not survive a process restart by design; use the SQLite repository for durable storage.
public class InMemoryBreweryRepository : IBreweryRepository
{
    private readonly object _lock = new();
    private IReadOnlyList<Brewery> _breweries = Array.Empty<Brewery>();
    private DateTimeOffset? _lastRefreshedUtc;

    public Task ReplaceAllAsync(IEnumerable<Brewery> breweries, DateTimeOffset refreshedAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _breweries = breweries.ToList();
            _lastRefreshedUtc = refreshedAtUtc;
        }

        return Task.CompletedTask;
    }

    public Task<BrewerySnapshot> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(new BrewerySnapshot { Items = _breweries, LastRefreshedUtc = _lastRefreshedUtc });
        }
    }

    public Task<BreweryQueryResult> QueryAsync(BreweryQueryParameters query, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var filtered = ApplyQuery(_breweries, query);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 500);
            var offset = (long)(page - 1) * pageSize;
            var items = offset > int.MaxValue
                ? new List<Brewery>()
                : filtered.Skip((int)offset).Take(pageSize).ToList();

            return Task.FromResult(new BreweryQueryResult
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = filtered.Count,
                LastRefreshedUtc = _lastRefreshedUtc ?? DateTimeOffset.UtcNow
            });
        }
    }

    private static List<Brewery> ApplyQuery(IEnumerable<Brewery> breweries, BreweryQueryParameters query)
    {
        IEnumerable<Brewery> result = breweries;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            result = result.Where(b => Contains(b.Name, term) || Contains(b.City, term) || Contains(b.State, term));
        }

        result = query.SortBy switch
        {
            BrewerySortBy.Name => query.SortDirection == SortDirection.Ascending
                ? result.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                : result.OrderByDescending(b => b.Name, StringComparer.OrdinalIgnoreCase),
            BrewerySortBy.City => query.SortDirection == SortDirection.Ascending
                ? result.OrderBy(b => b.City, StringComparer.OrdinalIgnoreCase)
                : result.OrderByDescending(b => b.City, StringComparer.OrdinalIgnoreCase),
            _ => result
        };

        return result.ToList();
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
