using Microsoft.EntityFrameworkCore;

namespace ElfBeauty.BreweryApi.Infrastructure.Data;

public class BreweryEntity
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
}

/// Single-row table tracking when the brewery data was last refreshed from the source API,
/// so the database can be read as a genuine cache-aside store (and survive app restarts)
/// rather than only ever being written to.
public class RefreshMetadataEntity
{
    public int Id { get; set; }
    public DateTimeOffset LastRefreshedUtc { get; set; }
}

/// EF Core DbContext backing the SQLite storage option (bonus: real database).
public class BreweryDbContext : DbContext
{
    public BreweryDbContext(DbContextOptions<BreweryDbContext> options) : base(options)
    {
    }

    public DbSet<BreweryEntity> Breweries => Set<BreweryEntity>();
    public DbSet<RefreshMetadataEntity> RefreshMetadata => Set<RefreshMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BreweryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.City).IsRequired();
        });

        modelBuilder.Entity<RefreshMetadataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}

