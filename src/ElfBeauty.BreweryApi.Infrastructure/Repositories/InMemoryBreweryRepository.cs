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
}
