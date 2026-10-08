using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using ElfBeauty.BreweryApi.Infrastructure.ExternalModels;

namespace ElfBeauty.BreweryApi.Infrastructure.Mapping;

/// Maps Open Brewery DB DTOs into the generic domain model.
public class BreweryMapper : IBreweryMapper<OpenBreweryDbDto>
{
    public Brewery Map(OpenBreweryDbDto source)
    {
        return new Brewery
        {
            Id = source.Id ?? Guid.NewGuid().ToString(),
            Name = source.Name ?? string.Empty,
            BreweryType = source.BreweryType,
            Street = source.Street ?? source.Address1,
            City = source.City ?? string.Empty,
            State = source.State ?? source.StateProvince,
            PostalCode = source.PostalCode,
            Country = source.Country,
            PhoneNumber = source.Phone,
            WebsiteUrl = source.WebsiteUrl,
            Latitude = source.Latitude,
            Longitude = source.Longitude
        };
    }
}
