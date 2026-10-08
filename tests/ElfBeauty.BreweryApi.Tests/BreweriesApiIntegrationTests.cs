using System.Net;
using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

/// End-to-end tests through the real ASP.NET Core pipeline (auth, versioning, middleware,
/// controller, DI) with only the external source faked out, so no live network calls are made.
public class BreweriesApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ApiKey = "integration-test-key";
    private readonly WebApplicationFactory<Program> _factory;

    public BreweriesApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKeySettings:ApiKey"] = ApiKey
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBreweryApiClient>();
                services.AddSingleton<IBreweryApiClient, FakeBreweryApiClient>();
            });
        });
    }

    [Fact]
    public async Task GetBreweries_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/breweries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBreweries_WithValidApiKey_Returns200WithData()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        var response = await client.GetAsync("/api/v1/breweries?pageSize=5");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Integration Test Brewing", body);
    }

    [Fact]
    public async Task GetBreweries_InvalidSortBy_Returns400()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        var response = await client.GetAsync("/api/v1/breweries?sortBy=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_DoesNotRequireApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    private sealed class FakeBreweryApiClient : IBreweryApiClient
    {
        public Task<IReadOnlyList<Brewery>> GetAllBreweriesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Brewery> breweries = new List<Brewery>
            {
                new() { Id = "1", Name = "Integration Test Brewing", City = "Testville", PhoneNumber = "5551234567" }
            };

            return Task.FromResult(breweries);
        }
    }
}
