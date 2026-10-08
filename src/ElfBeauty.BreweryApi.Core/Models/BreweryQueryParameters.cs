namespace ElfBeauty.BreweryApi.Core.Models;

/// Strongly typed query parameters accepted by the brewery listing endpoint.
public class BreweryQueryParameters
{
    public string? Search { get; set; }
    public BrewerySortBy? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// Reference point required when sorting/filtering by distance.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
