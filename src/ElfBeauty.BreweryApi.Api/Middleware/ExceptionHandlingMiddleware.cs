using ElfBeauty.BreweryApi.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ElfBeauty.BreweryApi.Api.Middleware;

/// Centralized error handling: converts known/unknown exceptions into consistent ProblemDetails responses.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidSortRequestException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Invalid request", ex.Message);
        }
        catch (BreweryDataSourceException ex)
        {
            _logger.LogError(ex, "Upstream brewery data source failure");
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, "Upstream data source error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "Please try again later.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return context.Response.WriteAsJsonAsync(problem);
    }
}
