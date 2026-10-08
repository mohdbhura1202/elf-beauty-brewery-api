namespace ElfBeauty.BreweryApi.Core.Models;

/// Snapshot returned by the repository: the stored breweries plus when they were last refreshed
/// from the source API (null if the store has never been populated).
public class BrewerySnapshot
{
    public IReadOnlyList<Brewery> Items { get; init; } = Array.Empty<Brewery>();
    public DateTimeOffset? LastRefreshedUtc { get; init; }
}
