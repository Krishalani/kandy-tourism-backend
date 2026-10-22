namespace KandyTourism.Api.Domain.Entities;

public class AttractionCategory
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Attraction> Attractions { get; set; } = [];
}
