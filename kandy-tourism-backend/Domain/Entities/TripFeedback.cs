namespace KandyTourism.Api.Domain.Entities;

/// <summary>The guest's review of a completed trip: one per itinerary.</summary>
public class TripFeedback
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int CommentMaxLength = 1000;

    public int Id { get; set; }
    public int ItineraryId { get; set; }
    public Itinerary Itinerary { get; set; } = null!;

    /// <summary>Overall rating for the day, 1–5 stars.</summary>
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public List<TripStopRating> StopRatings { get; set; } = [];
    public DateTime SubmittedAtUtc { get; set; }
}

/// <summary>An optional 1–5 star rating for one of the trip's stops.</summary>
public class TripStopRating
{
    public int Id { get; set; }
    public int TripFeedbackId { get; set; }
    public TripFeedback Feedback { get; set; } = null!;
    public int AttractionId { get; set; }
    public Attraction Attraction { get; set; } = null!;
    public int Rating { get; set; }
}
