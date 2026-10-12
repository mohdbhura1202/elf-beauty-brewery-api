namespace ElfBeauty.BreweryApi.Core.Models;

public class BreweryQueryResult
{
    public IReadOnlyList<Brewery> Items { get; init; } = Array.Empty<Brewery>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public DateTimeOffset LastRefreshedUtc { get; init; }
}
