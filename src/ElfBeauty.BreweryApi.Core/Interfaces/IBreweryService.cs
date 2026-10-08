using ElfBeauty.BreweryApi.Core.Models;

namespace ElfBeauty.BreweryApi.Core.Interfaces;

/// Orchestrates fetching, caching, searching, sorting and paging of brewery data.
public interface IBreweryService
{
    Task<PagedResult<Brewery>> GetBreweriesAsync(BreweryQueryParameters query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> AutocompleteAsync(string term, int maxResults, CancellationToken cancellationToken = default);
}
