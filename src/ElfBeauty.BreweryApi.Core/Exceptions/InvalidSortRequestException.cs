namespace ElfBeauty.BreweryApi.Core.Exceptions;

/// Thrown when the requested sort/query combination is invalid (e.g. distance sort without coordinates).
public class InvalidSortRequestException : Exception
{
    public InvalidSortRequestException(string message) : base(message)
    {
    }
}
