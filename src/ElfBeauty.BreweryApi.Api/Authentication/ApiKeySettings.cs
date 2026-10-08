namespace ElfBeauty.BreweryApi.Api.Authentication;

public class ApiKeySettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string HeaderName { get; set; } = "X-Api-Key";
}
