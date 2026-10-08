namespace ElfBeauty.BreweryApi.Core.Interfaces;

/// Maps a source-specific DTO to the generic domain model. Implemented per data source (OCP/DIP).
public interface IBreweryMapper<in TSource>
{
    Models.Brewery Map(TSource source);
}
