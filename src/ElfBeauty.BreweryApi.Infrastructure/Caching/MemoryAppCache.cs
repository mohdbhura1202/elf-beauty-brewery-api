using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ElfBeauty.BreweryApi.Infrastructure.Caching;

/// IMemoryCache-backed implementation of the generic app cache abstraction.
public class MemoryAppCache : IAppCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryAppCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
    {
        _memoryCache.Set(key, value, duration);
    }
}
