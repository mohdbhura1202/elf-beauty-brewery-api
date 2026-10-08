namespace ElfBeauty.BreweryApi.Core.Models;

/// Generic brewery representation, decoupled from any specific upstream data source.
public class Brewery
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BreweryType { get; set; }
    public string? Street { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// Populated only when a reference latitude/longitude is supplied for distance sorting.
    public double? DistanceInMiles { get; set; }

    /// Shallow copy used so per-request computations (e.g. distance) never mutate shared/cached instances.
    public Brewery Clone() => new()
    {
        Id = Id,
        Name = Name,
        BreweryType = BreweryType,
        Street = Street,
        City = City,
        State = State,
        PostalCode = PostalCode,
        Country = Country,
        PhoneNumber = PhoneNumber,
        WebsiteUrl = WebsiteUrl,
        Latitude = Latitude,
        Longitude = Longitude,
        DistanceInMiles = DistanceInMiles
    };
}
