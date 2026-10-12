using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using ElfBeauty.BreweryApi.Infrastructure.Options;
using ElfBeauty.BreweryApi.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

public class BreweryServiceTests
{
    private static List<Brewery> SampleBreweries() => new()
    {
        new Brewery { Id = "1", Name = "Alpha Brews", City = "Austin", State = "Texas", Latitude = 30.2672, Longitude = -97.7431 },
        new Brewery { Id = "2", Name = "Zephyr Ales", City = "Denver", State = "Colorado", Latitude = 39.7392, Longitude = -104.9903 },
        new Brewery { Id = "3", Name = "Beta Taproom", City = "Boulder", State = "Colorado", Latitude = 40.0150, Longitude = -105.2705 }
    };

    private static BreweryService CreateService(
        Mock<IBreweryApiClient> apiClient,
        Mock<IBreweryRepository> repository,
        Mock<IAppCache> cache)
    {
        var cacheOptions = Options.Create(new CacheOptions { DurationMinutes = 10 });
        return new BreweryService(apiClient.Object, repository.Object, cache.Object, cacheOptions, NullLogger<BreweryService>.Instance);
    }

    private static Mock<IAppCache> EmptyCache()
    {
        var cache = new Mock<IAppCache>();
        IReadOnlyList<Brewery> cachedList = Array.Empty<Brewery>();
        DateTimeOffset cachedTimestamp = default;
        cache.Setup(c => c.TryGetValue("breweries:all", out cachedList)).Returns(false);
        cache.Setup(c => c.TryGetValue("breweries:lastRefreshed", out cachedTimestamp)).Returns(false);
        return cache;
    }

    // Repository with nothing stored yet, so the service always falls through to the external API.
    private static Mock<IBreweryRepository> EmptyRepository()
    {
        var repository = new Mock<IBreweryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new BrewerySnapshot());
        repository.Setup(r => r.QueryAsync(It.IsAny<BreweryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BreweryQueryParameters query, CancellationToken _) => QuerySampleBreweries(query));
        return repository;
    }

    private static BreweryQueryResult QuerySampleBreweries(BreweryQueryParameters query)
    {
        IEnumerable<Brewery> result = SampleBreweries();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            result = result.Where(b => b.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       b.City.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       b.State!.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        result = query.SortBy switch
        {
            BrewerySortBy.Name when query.SortDirection == SortDirection.Ascending => result.OrderBy(b => b.Name),
            BrewerySortBy.Name => result.OrderByDescending(b => b.Name),
            BrewerySortBy.City when query.SortDirection == SortDirection.Ascending => result.OrderBy(b => b.City),
            BrewerySortBy.City => result.OrderByDescending(b => b.City),
            _ => result
        };

        var items = result.ToList();
        return new BreweryQueryResult { Items = items, Page = 1, PageSize = 50, TotalCount = items.Count };
    }

    [Fact]
    public async Task GetBreweriesAsync_FiltersBySearchTerm()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.GetBreweriesAsync(new BreweryQueryParameters { Search = "Denver" });

        Assert.Single(result.Items);
        Assert.Equal("Zephyr Ales", result.Items[0].Name);
    }

    [Fact]
    public async Task GetBreweriesAsync_SortsByNameAscending()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.GetBreweriesAsync(new BreweryQueryParameters { SortBy = BrewerySortBy.Name });

        Assert.Equal(new[] { "Alpha Brews", "Beta Taproom", "Zephyr Ales" }, result.Items.Select(b => b.Name));
    }

    [Fact]
    public async Task GetBreweriesAsync_SortByDistance_WithoutCoordinates_Throws()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        await Assert.ThrowsAsync<InvalidSortRequestException>(() =>
            service.GetBreweriesAsync(new BreweryQueryParameters { SortBy = BrewerySortBy.Distance }));
    }

    [Fact]
    public async Task GetBreweriesAsync_SortsByDistance_WhenCoordinatesProvided()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        // Reference point near Boulder, CO - Beta Taproom should be nearest.
        var result = await service.GetBreweriesAsync(new BreweryQueryParameters
        {
            SortBy = BrewerySortBy.Distance,
            Latitude = 40.0150,
            Longitude = -105.2705
        });

        Assert.Equal("Beta Taproom", result.Items.First().Name);
    }

    [Fact]
    public async Task AutocompleteAsync_PrioritizesStartsWithMatches()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.AutocompleteAsync("B", 10);

        Assert.Equal("Beta Taproom", result.First());
    }

    [Fact]
    public async Task GetBreweriesAsync_CombinesSearchAndSort()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.GetBreweriesAsync(new BreweryQueryParameters
        {
            Search = "Colorado",
            SortBy = BrewerySortBy.Name,
            SortDirection = SortDirection.Descending
        });

        Assert.Equal(new[] { "Zephyr Ales", "Beta Taproom" }, result.Items.Select(b => b.Name));
    }

    [Fact]
    public async Task GetBreweriesAsync_ReusesFreshlyStoredRepositoryData_WithoutCallingSourceApi()
    {
        // Regression test: the repository must be read as a genuine source on a cache miss
        // (e.g. after an app restart), not only written to or used as an on-failure fallback.
        var apiClient = new Mock<IBreweryApiClient>();
        var repository = new Mock<IBreweryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrewerySnapshot { Items = SampleBreweries(), LastRefreshedUtc = DateTimeOffset.UtcNow.AddMinutes(-2) });
        repository.Setup(r => r.QueryAsync(It.IsAny<BreweryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BreweryQueryParameters query, CancellationToken _) => QuerySampleBreweries(query));
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.GetBreweriesAsync(new BreweryQueryParameters());

        Assert.Equal(3, result.TotalCount);
        apiClient.Verify(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBreweriesAsync_FallsBackToRepository_WhenSourceApiFails()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BreweryDataSourceException("boom"));

        var repository = new Mock<IBreweryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrewerySnapshot { Items = SampleBreweries() });
        repository.Setup(r => r.QueryAsync(It.IsAny<BreweryQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BreweryQueryParameters query, CancellationToken _) => QuerySampleBreweries(query));

        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        var result = await service.GetBreweriesAsync(new BreweryQueryParameters());

        Assert.Equal(3, result.TotalCount);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public async Task GetBreweriesAsync_OutOfRangeCoordinates_Throws(double latitude, double longitude)
    {
        var apiClient = new Mock<IBreweryApiClient>();
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        await Assert.ThrowsAsync<InvalidSortRequestException>(() =>
            service.GetBreweriesAsync(new BreweryQueryParameters { Latitude = latitude, Longitude = longitude }));
    }

    [Fact]
    public async Task GetBreweriesAsync_DoesNotMutateSharedCachedInstances_WhenComputingDistance()
    {
        // Regression test: distance calculations must clone breweries, not mutate the shared cached list,
        // otherwise a later request without coordinates would leak stale DistanceInMiles values.
        var sample = SampleBreweries();
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sample);
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        await service.GetBreweriesAsync(new BreweryQueryParameters { Latitude = 40.0150, Longitude = -105.2705 });

        Assert.All(sample, b => Assert.Null(b.DistanceInMiles));
    }

    [Fact]
    public async Task GetBreweriesAsync_SecondCallWithoutCoordinates_DoesNotReturnStaleDistance()
    {
        var apiClient = new Mock<IBreweryApiClient>();
        apiClient.Setup(c => c.GetAllBreweriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleBreweries());
        var repository = EmptyRepository();
        var cache = EmptyCache();

        var service = CreateService(apiClient, repository, cache);

        await service.GetBreweriesAsync(new BreweryQueryParameters { Latitude = 40.0150, Longitude = -105.2705 });
        var secondResult = await service.GetBreweriesAsync(new BreweryQueryParameters());

        Assert.All(secondResult.Items, b => Assert.Null(b.DistanceInMiles));
    }
}
