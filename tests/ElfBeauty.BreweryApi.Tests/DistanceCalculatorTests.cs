using ElfBeauty.BreweryApi.Infrastructure.Services;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

public class DistanceCalculatorTests
{
    [Fact]
    public void CalculateMiles_ReturnsZero_ForSamePoint()
    {
        var distance = DistanceCalculator.CalculateMiles(39.7392, -104.9903, 39.7392, -104.9903);
        Assert.Equal(0, distance, 3);
    }

    [Fact]
    public void CalculateMiles_ReturnsApproximateDistance_BetweenKnownCities()
    {
        // Denver, CO to Boulder, CO is roughly 25-30 miles apart.
        var distance = DistanceCalculator.CalculateMiles(39.7392, -104.9903, 40.0150, -105.2705);
        Assert.InRange(distance, 20, 35);
    }
}
