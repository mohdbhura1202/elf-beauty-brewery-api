using Asp.Versioning;
using ElfBeauty.BreweryApi.Api.Contracts;
using ElfBeauty.BreweryApi.Core.Interfaces;
using ElfBeauty.BreweryApi.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ElfBeauty.BreweryApi.Api.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/breweries")]
public class BreweriesController : ControllerBase
{
    private readonly IBreweryService _breweryService;
    private readonly ILogger<BreweriesController> _logger;

    public BreweriesController(IBreweryService breweryService, ILogger<BreweriesController> logger)
    {
        _breweryService = breweryService;
        _logger = logger;
    }

    /// <summary>
    /// Returns open breweries (name, city, phone number and more) with optional search, sorting and paging.
    /// </summary>
    /// <param name="request">Search term, sort field/direction, distance origin and paging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Brewery>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<Brewery>>> GetBreweries([FromQuery] BreweryQueryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Brewery list requested. Search={Search} SortBy={SortBy}", request.Search, request.SortBy);

        var result = await _breweryService.GetBreweriesAsync(request.ToQueryParameters(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns brewery name suggestions for the supplied partial search term (search autocomplete).
    /// </summary>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<string>>> Autocomplete(
        [FromQuery, Required] string query,
        [FromQuery] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var suggestions = await _breweryService.AutocompleteAsync(query, maxResults, cancellationToken);
        return Ok(suggestions);
    }
}
