using ElfBeauty.BreweryApi.Infrastructure.ExternalModels;
using ElfBeauty.BreweryApi.Infrastructure.Mapping;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

public class BreweryMapperTests
{
    [Fact]
    public void Map_ParsesCoordinatesAndFields()
    {
        var mapper = new BreweryMapper();
        var dto = new OpenBreweryDbDto
        {
            Id = "abc-123",
            Name = "Test Brewing Co",
            City = "Denver",
            State = "Colorado",
            Phone = "3035551234",
            Latitude = 39.7392,
            Longitude = -104.9903
        };

        var result = mapper.Map(dto);

        Assert.Equal("abc-123", result.Id);
        Assert.Equal("Test Brewing Co", result.Name);
        Assert.Equal("Denver", result.City);
        Assert.Equal(39.7392, result.Latitude);
        Assert.Equal(-104.9903, result.Longitude);
    }

    [Fact]
    public void Map_HandlesMissingCoordinates()
    {
        var mapper = new BreweryMapper();
        var dto = new OpenBreweryDbDto { Id = "x", Name = "No Coords Brewing", City = "Austin", Latitude = null, Longitude = null };

        var result = mapper.Map(dto);

        Assert.Null(result.Latitude);
        Assert.Null(result.Longitude);
    }
}
