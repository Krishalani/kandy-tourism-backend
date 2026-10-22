namespace KandyTourism.Api.Domain.Entities;

/// <summary>An additional gallery photo. Images are hosted externally; only the URL is stored.</summary>
public class AttractionImage
{
    public int Id { get; set; }
    public int AttractionId { get; set; }
    public Attraction Attraction { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string? CreditLabel { get; set; }
    public string? CreditSourceUrl { get; set; }
    public int SortOrder { get; set; }
}
