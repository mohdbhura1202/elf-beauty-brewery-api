using ElfBeauty.BreweryApi.Core.Exceptions;
using ElfBeauty.BreweryApi.Core.Models;

namespace ElfBeauty.BreweryApi.Api.Contracts;

/// Query-string bound request DTO for the brewery listing endpoint.
public class BreweryQueryRequest
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public BreweryQueryParameters ToQueryParameters()
    {
        BrewerySortBy? sortBy = null;
        if (!string.IsNullOrWhiteSpace(SortBy))
        {
            if (!Enum.TryParse<BrewerySortBy>(SortBy, ignoreCase: true, out var parsedSortBy))
            {
                throw new InvalidSortRequestException($"Invalid sortBy value '{SortBy}'. Allowed values: Name, City, Distance.");
            }

            sortBy = parsedSortBy;
        }

        var sortDirection = Core.Models.SortDirection.Ascending;
        if (!string.IsNullOrWhiteSpace(SortDirection))
        {
            sortDirection = SortDirection.Trim().ToLowerInvariant() switch
            {
                "asc" => Core.Models.SortDirection.Ascending,
                "desc" => Core.Models.SortDirection.Descending,
                _ => Enum.TryParse<Core.Models.SortDirection>(SortDirection, ignoreCase: true, out var parsedDirection)
                    ? parsedDirection
                    : throw new InvalidSortRequestException($"Invalid sortDirection value '{SortDirection}'. Allowed values: asc, desc, Ascending, Descending.")
            };
        }

        return new BreweryQueryParameters
        {
            Search = Search,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Latitude = Latitude,
            Longitude = Longitude,
            Page = Page,
            PageSize = PageSize
        };
    }
}

