namespace KandyTourism.Api.Application.Common;

public enum DurationFilter
{
    Any,
    /// <summary>Under 60 minutes.</summary>
    Short,
    /// <summary>60 to 120 minutes inclusive.</summary>
    Medium,
    /// <summary>Over 120 minutes.</summary>
    Long,
}

public enum AttractionSort
{
    /// <summary>Featured first, then catalogue order.</summary>
    Recommended,
    Name,
    DurationAscending,
    DurationDescending,
}

/// <summary>Validated, normalised search parameters passed from the service to the repository.</summary>
/// <param name="CategorySlugs">Attractions in any of these categories match; empty means every category.</param>
public sealed record AttractionSearchCriteria(
    IReadOnlyList<string> Terms,
    IReadOnlyList<string> CategorySlugs,
    DurationFilter Duration,
    bool? Featured,
    AttractionSort Sort,
    int Page,
    int PageSize);
