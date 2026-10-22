using System.ComponentModel.DataAnnotations;
using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KandyTourism.Api.Controllers;

/// <summary>Public attraction catalogue. Only active attractions are returned.</summary>
[ApiController]
[Route("api/v1/attractions")]
public sealed class AttractionsController(IAttractionService attractions) : ControllerBase
{
    /// <summary>Lists attractions with keyword search, category/duration/featured filters, sorting and paging.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AttractionSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<AttractionSummaryDto>> List([FromQuery] AttractionListQuery query, CancellationToken ct) =>
        attractions.SearchAsync(query, ct);

    /// <summary>Gets an attraction's details by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AttractionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AttractionDetailsDto> GetById(int id, CancellationToken ct) => attractions.GetByIdAsync(id, ct);

    /// <summary>Gets an attraction's details by its URL slug (used by /attractions/:slug pages).</summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType<AttractionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AttractionDetailsDto> GetBySlug([StringLength(80)] string slug, CancellationToken ct) =>
        attractions.GetBySlugAsync(slug, ct);

    /// <summary>The closest other attractions by straight-line distance (not road distance).</summary>
    [HttpGet("{id:int}/nearby")]
    [ProducesResponseType<IReadOnlyList<NearbyAttractionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<NearbyAttractionDto>> GetNearby(int id, [FromQuery, Range(1, 12)] int limit = 3,
        CancellationToken ct = default) => attractions.GetNearbyAsync(id, limit, ct);
}

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(ICatalogService catalog) : ControllerBase
{
    /// <summary>Lists active attraction categories in display order.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoryDto>> List(CancellationToken ct) => catalog.GetCategoriesAsync(ct);
}

[ApiController]
[Route("api/v1/start-points")]
public sealed class StartPointsController(ICatalogService catalog) : ControllerBase
{
    /// <summary>Lists predefined starting points for a day plan.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StartPointDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<StartPointDto>> List(CancellationToken ct) => catalog.GetStartPointsAsync(ct);
}
