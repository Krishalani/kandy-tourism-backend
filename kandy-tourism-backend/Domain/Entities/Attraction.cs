namespace KandyTourism.Api.Domain.Entities;

public class Attraction
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public AttractionCategory Category { get; set; } = null!;

    public string ShortDescription { get; set; } = string.Empty;
    /// <summary>Description paragraphs in display order.</summary>
    public List<string> Description { get; set; } = [];

    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string? ImageCreditLabel { get; set; }
    public string? ImageCreditSourceUrl { get; set; }
    public List<AttractionImage> Gallery { get; set; } = [];

    public string Address { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>True when the coordinates are a best-effort estimate rather than a surveyed point.</summary>
    public bool CoordinatesApproximate { get; set; }

    /// <summary>Unverified planning estimate.</summary>
    public int? EstimatedVisitDurationMinutes { get; set; }
    /// <summary>Unverified estimate; never presented as official information.</summary>
    public string? OpeningHours { get; set; }
    /// <summary>Unverified estimate; never presented as official information.</summary>
    public string? EntranceFee { get; set; }

    public List<string> VisitorTips { get; set; } = [];
    public List<string> Tags { get; set; } = [];

    /// <summary>Normalised name, area, category and tags, used for database-side keyword search.</summary>
    public string SearchText { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }
    /// <summary>False once an admin deletes the attraction. Rows are kept so itineraries stay intact.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
