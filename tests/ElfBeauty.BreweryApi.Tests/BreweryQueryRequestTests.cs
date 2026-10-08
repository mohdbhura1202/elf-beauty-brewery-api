using ElfBeauty.BreweryApi.Api.Contracts;
using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Core.Models;
using Xunit;

namespace ElfBeauty.BreweryApi.Tests;

/// Validates request parsing: allowed sortBy/sortDirection values, aliases, and rejection of invalid input.
public class BreweryQueryRequestTests
{
    [Theory]
    [InlineData("asc", SortDirection.Ascending)]
    [InlineData("ASC", SortDirection.Ascending)]
    [InlineData("desc", SortDirection.Descending)]
    [InlineData("Descending", SortDirection.Descending)]
    [InlineData(null, SortDirection.Ascending)]
    public void ToQueryParameters_ParsesSortDirection(string? input, SortDirection expected)
    {
        var request = new BreweryQueryRequest { SortDirection = input };

        var result = request.ToQueryParameters();

        Assert.Equal(expected, result.SortDirection);
    }

    [Theory]
    [InlineData("name", BrewerySortBy.Name)]
    [InlineData("City", BrewerySortBy.City)]
    [InlineData("DISTANCE", BrewerySortBy.Distance)]
    public void ToQueryParameters_ParsesSortBy(string input, BrewerySortBy expected)
    {
        var request = new BreweryQueryRequest { SortBy = input };

        var result = request.ToQueryParameters();

        Assert.Equal(expected, result.SortBy);
    }

    [Fact]
    public void ToQueryParameters_InvalidSortBy_Throws()
    {
        var request = new BreweryQueryRequest { SortBy = "not-a-real-field" };

        Assert.Throws<InvalidSortRequestException>(() => request.ToQueryParameters());
    }

    [Fact]
    public void ToQueryParameters_InvalidSortDirection_Throws()
    {
        var request = new BreweryQueryRequest { SortDirection = "sideways" };

        Assert.Throws<InvalidSortRequestException>(() => request.ToQueryParameters());
    }

    [Fact]
    public void ToQueryParameters_SearchAndSort_CanBeCombined()
    {
        var request = new BreweryQueryRequest { Search = "ale", SortBy = "city", SortDirection = "desc" };

        var result = request.ToQueryParameters();

        Assert.Equal("ale", result.Search);
        Assert.Equal(BrewerySortBy.City, result.SortBy);
        Assert.Equal(SortDirection.Descending, result.SortDirection);
    }
}
