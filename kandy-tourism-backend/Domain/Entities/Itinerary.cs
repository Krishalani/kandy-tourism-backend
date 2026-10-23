using KandyTourism.Api.Domain.Enums;

namespace KandyTourism.Api.Domain.Entities;

/// <summary>
/// A guest's day plan. Guests are anonymous: access requires both the public ID and a secret
/// token, of which only a SHA-256 hash is stored.
/// </summary>
public class Itinerary
{
    public const int MaxStops = 10;

    public int Id { get; set; }
    public Guid PublicId { get; set; }
    public byte[] AccessTokenHash { get; set; } = [];

    public StartPointType StartType { get; set; }
    public int? StartPresetId { get; set; }
    public StartPreset? StartPreset { get; set; }
    public int? StartAttractionId { get; set; }
    public Attraction? StartAttraction { get; set; }
    /// <summary>Free-text start (e.g. a hotel name) passed to Google Maps as text.</summary>
    public string? StartQuery { get; set; }

    public List<ItineraryStop> Stops { get; set; } = [];

    /// <summary>The day the guest plans to make the trip (Sri Lanka date), if they have chosen one.</summary>
    public DateOnly? TripDate { get; set; }

    /// <summary>
    /// Public ID of a read-only share link, set while sharing is switched on. It is separate from
    /// <see cref="PublicId"/> so a shared link never grants edit access.
    /// </summary>
    public Guid? ShareId { get; set; }

    public ItineraryStatus Status { get; set; } = ItineraryStatus.Planning;
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>Set when the trip is completed.</summary>
    public TripFeedback? Feedback { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
