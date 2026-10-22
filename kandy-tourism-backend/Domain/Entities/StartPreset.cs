namespace KandyTourism.Api.Domain.Entities;

/// <summary>A predefined place a tourist can start the day from.</summary>
public class StartPreset
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool CoordinatesApproximate { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
