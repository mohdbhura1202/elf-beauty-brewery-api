using System.Net;
using System.Text;
using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Infrastructure.Http;
using ElfBeauty.BreweryApi.Infrastructure.Mapping;
using ElfBeauty.BreweryApi.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

/// Exercises the external API client against a stubbed HttpMessageHandler - no live network calls.
public class OpenBreweryDbApiClientTests
{
    private static OpenBreweryDbApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openbrewerydb.org/") };
        var options = Options.Create(new OpenBreweryDbOptions
        {
            BaseUrl = "https://api.openbrewerydb.org/v1/breweries",
            PageSize = 200,
            MaxPages = 1
        });

        return new OpenBreweryDbApiClient(httpClient, new BreweryMapper(), options, NullLogger<OpenBreweryDbApiClient>.Instance);
    }

    [Fact]
    public async Task GetAllBreweriesAsync_HttpFailure_WrapsAsBreweryDataSourceException()
    {
        var handler = new StubHttpMessageHandler(() => throw new HttpRequestException("network down"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<BreweryDataSourceException>(() => client.GetAllBreweriesAsync());
    }

    [Fact]
    public async Task GetAllBreweriesAsync_MalformedJson_WrapsAsBreweryDataSourceException()
    {
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ this is not valid json ]")
        });
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<BreweryDataSourceException>(() => client.GetAllBreweriesAsync());
    }

    [Fact]
    public async Task GetAllBreweriesAsync_ValidResponse_MapsToGenericModel()
    {
        const string json = "[{\"id\":\"abc\",\"name\":\"Test Brewing\",\"city\":\"Austin\",\"phone\":\"5551234567\",\"latitude\":30.1,\"longitude\":-97.2}]";
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.GetAllBreweriesAsync();

        Assert.Single(result);
        Assert.Equal("Test Brewing", result[0].Name);
        Assert.Equal("Austin", result[0].City);
        Assert.Equal(30.1, result[0].Latitude);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder());
        }
    }
}
