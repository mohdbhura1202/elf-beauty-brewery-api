namespace ElfBeauty.BreweryApi.Infrastructure.Options;

public class OpenBreweryDbOptions
{
    public string BaseUrl { get; set; } = "https://api.openbrewerydb.org/v1/breweries";
    public int PageSize { get; set; } = 200;
    public int MaxPages { get; set; } = 5;
}
