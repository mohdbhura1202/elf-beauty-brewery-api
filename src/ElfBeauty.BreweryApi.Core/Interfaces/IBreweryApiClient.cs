namespace ElfBeauty.BreweryApi.Core.Interfaces;

/// Abstraction over the upstream brewery data source (Open Brewery DB), already mapped to the generic model.
public interface IBreweryApiClient
{
    Task<IReadOnlyList<Models.Brewery>> GetAllBreweriesAsync(CancellationToken cancellationToken = default);
}
