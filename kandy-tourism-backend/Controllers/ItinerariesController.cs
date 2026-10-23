using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KandyTourism.Api.Controllers;

/// <summary>
/// Guest itineraries: no account needed. Every request except creation must send the access token
/// returned at creation in the <c>X-Itinerary-Token</c> header. Mutations return the full updated plan.
/// </summary>
[ApiController]
[Route("api/v1/itineraries")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public sealed class ItinerariesController(IItineraryService itineraries) : ControllerBase
{
    public const string TokenHeader = "X-Itinerary-Token";

    /// <summary>Creates an empty guest itinerary starting at the default start point.</summary>
    /// <remarks>The access token is returned only once; the client must store it.</remarks>
    [HttpPost]
    [ProducesResponseType<CreatedItineraryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedItineraryDto>> Create(CancellationToken ct)
    {
        var created = await itineraries.CreateAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = created.Itinerary.Id }, created);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> Get(Guid id, [FromHeader(Name = TokenHeader)] string? token, CancellationToken ct) =>
        itineraries.GetAsync(id, token, ct);

    /// <summary>Sets the starting point: a preset, one of the attractions, or a typed place.</summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<ItineraryDto> UpdateStart(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        UpdateStartRequest request, CancellationToken ct) => itineraries.UpdateStartAsync(id, token, request, ct);

    /// <summary>Adds an attraction at the end, or at <c>position</c> (0-based).</summary>
    [HttpPost("{id:guid}/stops")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ItineraryDto> AddStop(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        AddStopRequest request, CancellationToken ct) => itineraries.AddStopAsync(id, token, request, ct);

    /// <summary>Appends several attractions by slug, skipping duplicates and stopping at the stop limit.</summary>
    [HttpPost("{id:guid}/stops/bulk")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> AddStopsBySlug(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        AddStopsBySlugRequest request, CancellationToken ct) => itineraries.AddStopsBySlugAsync(id, token, request, ct);

    /// <summary>Replaces the visiting order. The list must contain every current stop exactly once.</summary>
    [HttpPut("{id:guid}/stops/order")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<ItineraryDto> Reorder(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        ReorderStopsRequest request, CancellationToken ct) => itineraries.ReorderStopsAsync(id, token, request, ct);

    [HttpDelete("{id:guid}/stops/{attractionId:int}")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> RemoveStop(Guid id, int attractionId, [FromHeader(Name = TokenHeader)] string? token,
        CancellationToken ct) => itineraries.RemoveStopAsync(id, token, attractionId, ct);

    /// <summary>Removes every stop; the starting point is kept.</summary>
    [HttpDelete("{id:guid}/stops")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> ClearStops(Guid id, [FromHeader(Name = TokenHeader)] string? token, CancellationToken ct) =>
        itineraries.ClearStopsAsync(id, token, ct);

    /// <summary>Sets the trip day (today up to a year ahead), or clears it with <c>null</c>.</summary>
    [HttpPut("{id:guid}/trip-date")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<ItineraryDto> UpdateTripDate(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        UpdateTripDateRequest request, CancellationToken ct) => itineraries.UpdateTripDateAsync(id, token, request, ct);

    /// <summary>Turns on a read-only share link (idempotent). The returned plan includes its <c>shareId</c>.</summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> EnableSharing(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        CancellationToken ct) => itineraries.EnableSharingAsync(id, token, ct);

    /// <summary>Turns the share link off; the old link stops working.</summary>
    [HttpDelete("{id:guid}/share")]
    [ProducesResponseType<ItineraryDto>(StatusCodes.Status200OK)]
    public Task<ItineraryDto> DisableSharing(Guid id, [FromHeader(Name = TokenHeader)] string? token,
        CancellationToken ct) => itineraries.DisableSharingAsync(id, token, ct);
}

/// <summary>Read-only access to plans their owners have chosen to share. No token is needed.</summary>
[ApiController]
[Route("api/v1/shared-itineraries")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SharedItinerariesController(IItineraryService itineraries) : ControllerBase
{
    [HttpGet("{shareId:guid}")]
    [ProducesResponseType<SharedItineraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SharedItineraryDto> Get(Guid shareId, CancellationToken ct) => itineraries.GetSharedAsync(shareId, ct);
}
