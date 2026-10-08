namespace ElfBeauty.BreweryApi.Infrastructure.Options;

public class StorageOptions
{
    /// "InMemory" (default) or "Sqlite".
    public string Provider { get; set; } = "InMemory";
}
