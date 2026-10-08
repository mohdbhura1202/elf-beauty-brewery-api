using System.Net.Http.Json;
using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using ElfBeauty.BreweryApi.Infrastructure.ExternalModels;
using ElfBeauty.BreweryApi.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElfBeauty.BreweryApi.Infrastructure.Http;

/// Fetches brewery data from the public Open Brewery DB REST API, paging through results.
public class OpenBreweryDbApiClient : IBreweryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IBreweryMapper<OpenBreweryDbDto> _mapper;
    private readonly OpenBreweryDbOptions _options;
    private readonly ILogger<OpenBreweryDbApiClient> _logger;

    public OpenBreweryDbApiClient(
        HttpClient httpClient,
        IBreweryMapper<OpenBreweryDbDto> mapper,
        IOptions<OpenBreweryDbOptions> options,
        ILogger<OpenBreweryDbApiClient> logger)
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Brewery>> GetAllBreweriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<Brewery>();

        try
        {
            for (var page = 1; page <= _options.MaxPages; page++)
            {
                var requestUri = $"{_options.BaseUrl}?page={page}&per_page={_options.PageSize}";
                _logger.LogInformation("Fetching brewery data page {Page} from source API", page);

                var dtos = await _httpClient.GetFromJsonAsync<List<OpenBreweryDbDto>>(requestUri, cancellationToken);

                if (dtos is null || dtos.Count == 0)
                {
                    break;
                }

                results.AddRange(dtos.Select(_mapper.Map));

                if (dtos.Count < _options.PageSize)
                {
                    break;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach the Open Brewery DB source API");
            throw new BreweryDataSourceException("Unable to reach the brewery source API.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Open Brewery DB source API request timed out");
            throw new BreweryDataSourceException("The brewery source API request timed out.", ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse response from the Open Brewery DB source API");
            throw new BreweryDataSourceException("Received an unexpected response from the brewery source API.", ex);
        }

        _logger.LogInformation("Fetched {Count} breweries from source API", results.Count);
        return results;
    }
}
