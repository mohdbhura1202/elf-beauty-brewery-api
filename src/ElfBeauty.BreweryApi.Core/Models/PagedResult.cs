namespace ElfBeauty.BreweryApi.Core.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    /// When the underlying data was last refreshed from the source API (cache timestamp).
    public DateTimeOffset LastRefreshedUtc { get; init; }
}
