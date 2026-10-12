using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using ElfBeauty.BreweryApi.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElfBeauty.BreweryApi.Infrastructure.Services;

/// Orchestrates caching, searching, sorting and paging of brewery data (single responsibility: query orchestration).
public class BreweryService : IBreweryService
{
    private const string CacheKey = "breweries:all";
    private const string CacheTimestampKey = "breweries:lastRefreshed";
    private const string StaleCacheKey = "breweries:stale";
    private const string StaleCacheTimestampKey = "breweries:stale:lastRefreshed";

    // Guards against a cache-stampede: without this, many concurrent requests during a cache
    // miss would each independently call the external API and write to storage at once.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly IBreweryApiClient _apiClient;
    private readonly IBreweryRepository _repository;
    private readonly IAppCache _cache;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<BreweryService> _logger;

    public BreweryService(
        IBreweryApiClient apiClient,
        IBreweryRepository repository,
        IAppCache cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<BreweryService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<PagedResult<Brewery>> GetBreweriesAsync(BreweryQueryParameters query, CancellationToken cancellationToken = default)
    {
        if (query.SortBy == BrewerySortBy.Distance && (query.Latitude is null || query.Longitude is null))
        {
            throw new InvalidSortRequestException("Sorting by distance requires both 'latitude' and 'longitude' query parameters.");
        }

        if (query.Latitude is < -90 or > 90)
        {
            throw new InvalidSortRequestException("Latitude must be between -90 and 90.");
        }

        if (query.Longitude is < -180 or > 180)
        {
            throw new InvalidSortRequestException("Longitude must be between -180 and 180.");
        }

        var (all, lastRefreshed) = await GetAllBreweriesAsync(cancellationToken);

        if (query.SortBy != BrewerySortBy.Distance && query.Latitude is null && query.Longitude is null)
        {
            var storedResult = await _repository.QueryAsync(query, cancellationToken);
            return new PagedResult<Brewery>
            {
                Items = storedResult.Items,
                Page = storedResult.Page,
                PageSize = storedResult.PageSize,
                TotalCount = storedResult.TotalCount,
                LastRefreshedUtc = lastRefreshed
            };
        }

        IEnumerable<Brewery> filtered = all;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(b =>
                Contains(b.Name, term) ||
                Contains(b.City, term) ||
                Contains(b.State, term));
        }

        if (query.Latitude is not null && query.Longitude is not null)
        {
            filtered = filtered.Select(b => WithDistance(b, query.Latitude.Value, query.Longitude.Value));
        }

        filtered = ApplySort(filtered, query.SortBy, query.SortDirection);

        var materialized = filtered.ToList();
        var totalCount = materialized.Count;

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);

        var pageItems = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Brewery>
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            LastRefreshedUtc = lastRefreshed
        };
    }

    public async Task<IReadOnlyList<string>> AutocompleteAsync(string term, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<string>();
        }

        var (all, _) = await GetAllBreweriesAsync(cancellationToken);
        var trimmedTerm = term.Trim();

        var startsWith = all
            .Where(b => b.Name.StartsWith(trimmedTerm, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Name);

        var contains = all
            .Where(b => !b.Name.StartsWith(trimmedTerm, StringComparison.OrdinalIgnoreCase) &&
                        b.Name.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Name);

        return startsWith.Concat(contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxResults))
            .ToList();
    }

    private async Task<(IReadOnlyList<Brewery> All, DateTimeOffset LastRefreshed)> GetAllBreweriesAsync(CancellationToken cancellationToken)
    {
        if (TryGetCached(out var cached, out var cachedTimestamp))
        {
            return (cached, cachedTimestamp);
        }

        if (TryGetStaleCached(out var stale, out var staleTimestamp))
        {
            _logger.LogInformation("Serving stale brewery cache while refreshing in the background (refreshed {Timestamp})", staleTimestamp);
            _ = RefreshCacheInBackgroundAsync();
            return (stale, staleTimestamp);
        }

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check: another request may have already refreshed the cache while we waited.
            if (TryGetCached(out cached, out cachedTimestamp))
            {
                return (cached, cachedTimestamp);
            }

            var cacheDuration = TimeSpan.FromMinutes(_cacheOptions.DurationMinutes);

            // The repository is read here (not just written to): if the store already holds data
            // refreshed within the cache window - e.g. after an app restart wiped the in-process
            // cache - we reuse it directly instead of unnecessarily calling the external API again.
            var stored = await _repository.GetAllAsync(cancellationToken);
            if (stored is { Items.Count: > 0 } && stored.LastRefreshedUtc is { } storedTimestamp &&
                DateTimeOffset.UtcNow - storedTimestamp < cacheDuration)
            {
                _logger.LogInformation("Reusing {Count} breweries already persisted in storage (refreshed {Timestamp})", stored.Items.Count, storedTimestamp);
                _cache.Set(CacheKey, stored.Items, cacheDuration);
                _cache.Set(CacheTimestampKey, storedTimestamp, cacheDuration);
                SetStaleCache(stored.Items, storedTimestamp, cacheDuration);
                return (stored.Items, storedTimestamp);
            }

            _logger.LogInformation("Brewery cache miss or expired - refreshing from source API");
            var fresh = await _apiClient.GetAllBreweriesAsync(cancellationToken);
            var refreshedAt = DateTimeOffset.UtcNow;
            await _repository.ReplaceAllAsync(fresh, refreshedAt, cancellationToken);

            _cache.Set(CacheKey, fresh, cacheDuration);
            _cache.Set(CacheTimestampKey, refreshedAt, cacheDuration);
            SetStaleCache(fresh, refreshedAt, cacheDuration);

            return (fresh, refreshedAt);
        }
        catch (BreweryDataSourceException ex)
        {
            _logger.LogWarning(ex, "Source API unavailable, attempting to fall back to stored data");
            var fallback = await _repository.GetAllAsync(cancellationToken);

            if (fallback is not { Items.Count: > 0 })
            {
                throw;
            }

            _logger.LogWarning("Falling back to {Count} previously stored breweries", fallback.Items.Count);
            return (fallback.Items, fallback.LastRefreshedUtc ?? DateTimeOffset.UtcNow);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private bool TryGetCached(out IReadOnlyList<Brewery> cached, out DateTimeOffset cachedTimestamp)
    {
        cached = Array.Empty<Brewery>();
        cachedTimestamp = default;

        if (_cache.TryGetValue(CacheKey, out cached) && _cache.TryGetValue(CacheTimestampKey, out cachedTimestamp))
        {
            _logger.LogDebug("Serving {Count} breweries from cache (refreshed {Timestamp})", cached.Count, cachedTimestamp);
            return true;
        }

        return false;
    }

    private bool TryGetStaleCached(out IReadOnlyList<Brewery> cached, out DateTimeOffset cachedTimestamp)
    {
        cached = Array.Empty<Brewery>();
        cachedTimestamp = default;

        return _cache.TryGetValue(StaleCacheKey, out cached) &&
               _cache.TryGetValue(StaleCacheTimestampKey, out cachedTimestamp);
    }

    private void SetStaleCache(IReadOnlyList<Brewery> breweries, DateTimeOffset refreshedAt, TimeSpan freshDuration)
    {
        var staleDuration = TimeSpan.FromMinutes(Math.Max(freshDuration.TotalMinutes * 3, freshDuration.TotalMinutes + 1));
        _cache.Set(StaleCacheKey, breweries, staleDuration);
        _cache.Set(StaleCacheTimestampKey, refreshedAt, staleDuration);
    }

    private async Task RefreshCacheInBackgroundAsync()
    {
        if (!await RefreshLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            var fresh = await _apiClient.GetAllBreweriesAsync();
            var refreshedAt = DateTimeOffset.UtcNow;
            var duration = TimeSpan.FromMinutes(_cacheOptions.DurationMinutes);
            await _repository.ReplaceAllAsync(fresh, refreshedAt);
            _cache.Set(CacheKey, fresh, duration);
            _cache.Set(CacheTimestampKey, refreshedAt, duration);
            SetStaleCache(fresh, refreshedAt, duration);
            _logger.LogInformation("Background brewery cache refresh completed with {Count} records", fresh.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background brewery cache refresh failed; stale data remains available");
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    // Returns a clone so distance calculations never mutate shared cached/repository instances.
    private static Brewery WithDistance(Brewery brewery, double latitude, double longitude)
    {
        var clone = brewery.Clone();

        if (clone.Latitude is not null && clone.Longitude is not null)
        {
            clone.DistanceInMiles = DistanceCalculator.CalculateMiles(latitude, longitude, clone.Latitude.Value, clone.Longitude.Value);
        }

        return clone;
    }

    private static IEnumerable<Brewery> ApplySort(IEnumerable<Brewery> breweries, BrewerySortBy? sortBy, SortDirection direction)
    {
        var ascending = direction == SortDirection.Ascending;

        return sortBy switch
        {
            BrewerySortBy.Name => ascending ? breweries.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                                             : breweries.OrderByDescending(b => b.Name, StringComparer.OrdinalIgnoreCase),
            BrewerySortBy.City => ascending ? breweries.OrderBy(b => b.City, StringComparer.OrdinalIgnoreCase)
                                             : breweries.OrderByDescending(b => b.City, StringComparer.OrdinalIgnoreCase),
            BrewerySortBy.Distance => ascending ? breweries.OrderBy(b => b.DistanceInMiles ?? double.MaxValue)
                                                 : breweries.OrderByDescending(b => b.DistanceInMiles ?? double.MinValue),
            _ => breweries
        };
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
