namespace ElfBeauty.BreweryApi.Core.Interfaces;

/// Thin cache abstraction so the service layer does not depend on a specific caching implementation.
public interface IAppCache
{
    bool TryGetValue<T>(string key, out T value);
    void Set<T>(string key, T value, TimeSpan duration);
}
