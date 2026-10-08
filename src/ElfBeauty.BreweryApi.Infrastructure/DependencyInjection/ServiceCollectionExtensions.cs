using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Infrastructure.Caching;
using ElfBeauty.BreweryApi.Infrastructure.Data;
using ElfBeauty.BreweryApi.Infrastructure.ExternalModels;
using ElfBeauty.BreweryApi.Infrastructure.Http;
using ElfBeauty.BreweryApi.Infrastructure.Mapping;
using ElfBeauty.BreweryApi.Infrastructure.Options;
using ElfBeauty.BreweryApi.Infrastructure.Repositories;
using ElfBeauty.BreweryApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElfBeauty.BreweryApi.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBreweryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenBreweryDbOptions>(configuration.GetSection("OpenBreweryDb"));
        services.Configure<CacheOptions>(configuration.GetSection("Cache"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        services.AddMemoryCache();
        services.AddSingleton<IAppCache, MemoryAppCache>();
        services.AddSingleton<IBreweryMapper<OpenBreweryDbDto>, BreweryMapper>();

        services.AddHttpClient<IBreweryApiClient, OpenBreweryDbApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var storageOptions = configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions();

        if (string.Equals(storageOptions.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<BreweryDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("BreweryDb")));
            services.AddScoped<IBreweryRepository, SqliteBreweryRepository>();
        }
        else
        {
            services.AddSingleton<IBreweryRepository, InMemoryBreweryRepository>();
        }

        services.AddScoped<IBreweryService, BreweryService>();

        return services;
    }
}
