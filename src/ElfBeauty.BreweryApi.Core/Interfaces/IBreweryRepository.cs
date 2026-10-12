namespace ElfBeauty.BreweryApi.Core.Interfaces;

/// Storage abstraction. Swappable between in-memory and a real database (e.g. SQLite + EF Core).
/// The repository is a genuine read/write store: on a cache miss, the service reads from here
/// before deciding whether to call the external source, so the store is never write-only.
public interface IBreweryRepository
{
    Task ReplaceAllAsync(IEnumerable<Models.Brewery> breweries, DateTimeOffset refreshedAtUtc, CancellationToken cancellationToken = default);
    Task<Models.BrewerySnapshot> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Models.BreweryQueryResult> QueryAsync(Models.BreweryQueryParameters query, CancellationToken cancellationToken = default);
}
