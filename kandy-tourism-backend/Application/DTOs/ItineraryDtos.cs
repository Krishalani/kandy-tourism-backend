using System.ComponentModel.DataAnnotations;
using KandyTourism.Api.Domain.Enums;
using ItineraryEntity = KandyTourism.Api.Domain.Entities.Itinerary;

namespace KandyTourism.Api.Application.DTOs;

/// <summary>
/// The resolved starting point. <see cref="Latitude"/>/<see cref="Longitude"/> are absent for a
/// typed (custom) start, which Google Maps receives as text.
/// </summary>
public sealed record ItineraryStartDto
{
    public StartPointType Type { get; init; }
    public string? PresetSlug { get; init; }
    public int? AttractionId { get; init; }
    public string? Query { get; init; }
    public string Label { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public bool CoordinatesApproximate { get; init; }
}

public sealed record ItineraryStopDto(int Order, AttractionSummaryDto Attraction);

public sealed record ItineraryDto
{
    public Guid Id { get; init; }
    public ItineraryStartDto Start { get; init; } = new();
    /// <summary>Stops in the tourist's chosen visiting order.</summary>
    public IReadOnlyList<ItineraryStopDto> Stops { get; init; } = [];
    public int MaxStops { get; init; }
    /// <summary>The planned day of the trip, if chosen.</summary>
    public DateOnly? TripDate { get; init; }
    /// <summary>Set while a read-only share link is active (<c>GET /api/v1/shared-itineraries/{shareId}</c>).</summary>
    public Guid? ShareId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

/// <summary>A read-only view of a shared plan. It carries no ID or token that could change the plan.</summary>
public sealed record SharedItineraryDto
{
    public Guid ShareId { get; init; }
    public ItineraryStartDto Start { get; init; } = new();
    public IReadOnlyList<ItineraryStopDto> Stops { get; init; } = [];
    public DateOnly? TripDate { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed record UpdateTripDateRequest
{
    /// <summary>The trip day (yyyy-MM-dd), from today up to a year ahead; null clears it.</summary>
    public DateOnly? TripDate { get; init; }
}

/// <summary>Returned once, on creation. The client must keep the token to access the plan.</summary>
public sealed record CreatedItineraryDto(string AccessToken, ItineraryDto Itinerary);

public sealed record AddStopRequest
{
    [Range(1, int.MaxValue)]
    public int AttractionId { get; init; }

    /// <summary>Optional 0-based insert position (used to undo a removal). Defaults to the end.</summary>
    [Range(0, ItineraryEntity.MaxStops)]
    public int? Position { get; init; }
}

public sealed record AddStopsBySlugRequest
{
    /// <summary>Attraction slugs in visiting order. Duplicates and unknown slugs are skipped.</summary>
    [Required, MinLength(1), MaxLength(ItineraryEntity.MaxStops)]
    public List<string> AttractionSlugs { get; init; } = [];
}

public sealed record ReorderStopsRequest
{
    /// <summary>Every current stop's attraction ID, exactly once, in the new order.</summary>
    [Required, MaxLength(ItineraryEntity.MaxStops)]
    public List<int> AttractionIds { get; init; } = [];
}

public sealed record UpdateStartRequest
{
    [Required]
    public StartPointType? Type { get; init; }

    [StringLength(50)]
    public string? PresetSlug { get; init; }

    public int? AttractionId { get; init; }

    [StringLength(200)]
    public string? Query { get; init; }
}