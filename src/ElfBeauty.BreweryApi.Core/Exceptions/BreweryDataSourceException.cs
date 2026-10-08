namespace ElfBeauty.BreweryApi.Core.Exceptions;

/// Thrown when the upstream Open Brewery DB API cannot be reached or returns an unexpected response.
public class BreweryDataSourceException : Exception
{
    public BreweryDataSourceException(string message) : base(message)
    {
    }

    public BreweryDataSourceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
