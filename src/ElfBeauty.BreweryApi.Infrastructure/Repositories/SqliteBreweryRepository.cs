using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using ElfBeauty.BreweryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ElfBeauty.BreweryApi.Infrastructure.Repositories;

/// Bonus storage implementation backed by SQLite via EF Core, swappable with <see cref="InMemoryBreweryRepository"/>.
/// Persists a refresh timestamp alongside the data so it is a genuine read/write cache-aside
/// store (durable across process restarts), not a write-only destination.
public class SqliteBreweryRepository : IBreweryRepository
{
    private const int MetadataRowId = 1;

    private readonly BreweryDbContext _dbContext;

    public SqliteBreweryRepository(BreweryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplaceAllAsync(IEnumerable<Brewery> breweries, DateTimeOffset refreshedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Breweries.RemoveRange(_dbContext.Breweries);
        await _dbContext.Breweries.AddRangeAsync(breweries.Select(ToEntity), cancellationToken);

        var metadata = await _dbContext.RefreshMetadata.FindAsync(new object[] { MetadataRowId }, cancellationToken);
        if (metadata is null)
        {
            _dbContext.RefreshMetadata.Add(new RefreshMetadataEntity { Id = MetadataRowId, LastRefreshedUtc = refreshedAtUtc });
        }
        else
        {
            metadata.LastRefreshedUtc = refreshedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<BrewerySnapshot> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Breweries.AsNoTracking().ToListAsync(cancellationToken);
        var metadata = await _dbContext.RefreshMetadata.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MetadataRowId, cancellationToken);

        return new BrewerySnapshot
        {
            Items = entities.Select(ToModel).ToList(),
            LastRefreshedUtc = metadata?.LastRefreshedUtc
        };
    }

    private static BreweryEntity ToEntity(Brewery brewery) => new()
    {
        Id = brewery.Id,
        Name = brewery.Name,
        BreweryType = brewery.BreweryType,
        Street = brewery.Street,
        City = brewery.City,
        State = brewery.State,
        PostalCode = brewery.PostalCode,
        Country = brewery.Country,
        PhoneNumber = brewery.PhoneNumber,
        WebsiteUrl = brewery.WebsiteUrl,
        Latitude = brewery.Latitude,
        Longitude = brewery.Longitude
    };

    private static Brewery ToModel(BreweryEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        BreweryType = entity.BreweryType,
        Street = entity.Street,
        City = entity.City,
        State = entity.State,
        PostalCode = entity.PostalCode,
        Country = entity.Country,
        PhoneNumber = entity.PhoneNumber,
        WebsiteUrl = entity.WebsiteUrl,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude
    };
}
