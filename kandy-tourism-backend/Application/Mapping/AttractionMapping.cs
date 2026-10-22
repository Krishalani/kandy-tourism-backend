using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Domain.Entities;

namespace KandyTourism.Api.Application.Mapping;

public static class AttractionMapping
{
    /// <remarks>Requires <see cref="Attraction.Category"/> to be loaded.</remarks>
    public static AttractionSummaryDto ToSummaryDto(this Attraction a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Slug = a.Slug,
        Category = a.Category.Slug,
        CategoryName = a.Category.Name,
        ShortDescription = a.ShortDescription,
        ImageUrl = a.ImageUrl,
        ImageAlt = a.ImageAlt,
        Area = a.Area,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        CoordinatesApproximate = a.CoordinatesApproximate,
        EstimatedVisitDurationMinutes = a.EstimatedVisitDurationMinutes,
        Featured = a.IsFeatured,
    };

    /// <remarks>Requires <see cref="Attraction.Category"/> and <see cref="Attraction.Gallery"/> to be loaded.</remarks>
    public static AttractionDetailsDto ToDetailsDto(this Attraction a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Slug = a.Slug,
        Category = a.Category.Slug,
        CategoryName = a.Category.Name,
        ShortDescription = a.ShortDescription,
        ImageUrl = a.ImageUrl,
        ImageAlt = a.ImageAlt,
        Area = a.Area,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        CoordinatesApproximate = a.CoordinatesApproximate,
        EstimatedVisitDurationMinutes = a.EstimatedVisitDurationMinutes,
        Featured = a.IsFeatured,
        Description = a.Description,
        ImageCredit = ToCredit(a.ImageCreditLabel, a.ImageCreditSourceUrl),
        Gallery = a.Gallery
            .OrderBy(g => g.SortOrder)
            .Select(g => new GalleryImageDto { Url = g.Url, Alt = g.AltText, Credit = ToCredit(g.CreditLabel, g.CreditSourceUrl) })
            .ToList(),
        Address = a.Address,
        OpeningHours = a.OpeningHours,
        EntranceFee = a.EntranceFee,
        VisitorTips = a.VisitorTips,
        Tags = a.Tags,
    };

    private static ImageCreditDto? ToCredit(string? label, string? sourceUrl) =>
        label is null || sourceUrl is null ? null : new ImageCreditDto { Label = label, SourceUrl = sourceUrl };
}
