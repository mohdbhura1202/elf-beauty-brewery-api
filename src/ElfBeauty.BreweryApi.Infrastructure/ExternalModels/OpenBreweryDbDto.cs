using System.Text.Json.Serialization;

namespace ElfBeauty.BreweryApi.Infrastructure.ExternalModels;

/// Raw DTO shape returned by https://api.openbrewerydb.org/v1/breweries.
public class OpenBreweryDbDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("brewery_type")]
    public string? BreweryType { get; set; }

    [JsonPropertyName("address_1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("longitude")]
    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]
    public double? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    [JsonConverter(typeof(FlexibleNullableDoubleConverter))]
    public double? Latitude { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }
}
