namespace KandyTourism.Api.Domain.Entities;

public class ItineraryStop
{
    public int Id { get; set; }
    public int ItineraryId { get; set; }
    public Itinerary Itinerary { get; set; } = null!;
    public int AttractionId { get; set; }
    public Attraction Attraction { get; set; } = null!;
    /// <summary>1-based position in the tourist's chosen order; unique within an itinerary.</summary>
    public int Sequence { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
