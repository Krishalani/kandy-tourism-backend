using System.ComponentModel.DataAnnotations;

namespace KandyTourism.Api.Application.DTOs;

public sealed record ImageCreditDto
{
    [Required, StringLength(100)]
    public string Label { get; init; } = string.Empty;

    [Required, StringLength(500), Url]
    public string SourceUrl { get; init; } = string.Empty;
}

public sealed record GalleryImageDto
{
    [Required, StringLength(500), RegularExpression(ValidationPatterns.ImageUrl, ErrorMessage = ValidationPatterns.ImageUrlMessage)]
    public string Url { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Alt { get; init; } = string.Empty;

    public ImageCreditDto? Credit { get; init; }
}

/// <summary>Card/list representation, also embedded in itinerary stops.</summary>
public record AttractionSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    /// <summary>Category slug, e.g. "religious".</summary>
    public string Category { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string ImageAlt { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public bool CoordinatesApproximate { get; init; }
    public int? EstimatedVisitDurationMinutes { get; init; }
    public bool Featured { get; init; }
}

public sealed record AttractionDetailsDto : AttractionSummaryDto
{
    public IReadOnlyList<string> Description { get; init; } = [];
    public ImageCreditDto? ImageCredit { get; init; }
    public IReadOnlyList<GalleryImageDto> Gallery { get; init; } = [];
    public string Address { get; init; } = string.Empty;
    public string? OpeningHours { get; init; }
    public string? EntranceFee { get; init; }
    public IReadOnlyList<string> VisitorTips { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record NearbyAttractionDto(AttractionSummaryDto Attraction, double DistanceKm);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>Query string for <c>GET /api/v1/attractions</c>.</summary>
public sealed record AttractionListQuery
{
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 100;

    /// <summary>Keywords matched against name, area, category and tags (all words must match).</summary>
    [StringLength(100)]
    public string? Q { get; init; }

    /// <summary>Category slug. Omit or use "all" for every category.</summary>
    [StringLength(50)]
    public string? Category { get; init; }

    /// <summary>any | short (&lt; 1 hr) | medium (1–2 hrs) | long (&gt; 2 hrs).</summary>
    [RegularExpression("^(any|short|medium|long)$", ErrorMessage = "Duration must be one of: any, short, medium, long.")]
    public string? Duration { get; init; }

    public bool? Featured { get; init; }

    /// <summary>recommended | name | duration-asc | duration-desc.</summary>
    [RegularExpression("^(recommended|name|duration-asc|duration-desc)$",
        ErrorMessage = "Sort must be one of: recommended, name, duration-asc, duration-desc.")]
    public string? Sort { get; init; }

    [Range(1, 10_000)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}

/// <summary>Create/update payload used by the admin attraction form.</summary>
public sealed record AttractionUpsertRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(80), RegularExpression(ValidationPatterns.Slug, ErrorMessage = ValidationPatterns.SlugMessage)]
    public string Slug { get; init; } = string.Empty;

    /// <summary>Category slug.</summary>
    [Required, StringLength(50)]
    public string Category { get; init; } = string.Empty;

    [Required, StringLength(220)]
    public string ShortDescription { get; init; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Add at least one description paragraph."), MaxLength(20)]
    public List<string> Description { get; init; } = [];

    [Required, StringLength(500), RegularExpression(ValidationPatterns.ImageUrl, ErrorMessage = ValidationPatterns.ImageUrlMessage)]
    public string ImageUrl { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string ImageAlt { get; init; } = string.Empty;

    public ImageCreditDto? ImageCredit { get; init; }

    [MaxLength(12)]
    public List<GalleryImageDto> Gallery { get; init; } = [];

    [Required, StringLength(160)]
    public string Address { get; init; } = string.Empty;

    [Required, StringLength(80)]
    public string Area { get; init; } = string.Empty;

    [Range(ValidationPatterns.SriLankaMinLatitude, ValidationPatterns.SriLankaMaxLatitude,
        ErrorMessage = "Latitude should be between 5.8 and 10 for Sri Lanka.")]
    public double Latitude { get; init; }

    [Range(ValidationPatterns.SriLankaMinLongitude, ValidationPatterns.SriLankaMaxLongitude,
        ErrorMessage = "Longitude should be between 79.4 and 82 for Sri Lanka.")]
    public double Longitude { get; init; }

    public bool CoordinatesApproximate { get; init; }

    [Range(5, 720, ErrorMessage = "Enter a visit time between 5 and 720 minutes.")]
    public int? EstimatedVisitDurationMinutes { get; init; }

    [StringLength(200)]
    public string? OpeningHours { get; init; }

    [StringLength(200)]
    public string? EntranceFee { get; init; }

    [MaxLength(20)]
    public List<string> VisitorTips { get; init; } = [];

    public bool Featured { get; init; }

    [MaxLength(30)]
    public List<string> Tags { get; init; } = [];
}

public static class ValidationPatterns
{
    public const string Slug = "^[a-z0-9]+(?:-[a-z0-9]+)*$";
    public const string SlugMessage = "Use lowercase letters, numbers and single hyphens, e.g. kandy-lake.";
    public const string ImageUrl = @"^(https://\S+|/\S+)$";
    public const string ImageUrlMessage = "Enter an https:// URL or a path starting with /.";

    public const double SriLankaMinLatitude = 5.8;
    public const double SriLankaMaxLatitude = 10;
    public const double SriLankaMinLongitude = 79.4;
    public const double SriLankaMaxLongitude = 82;
}
