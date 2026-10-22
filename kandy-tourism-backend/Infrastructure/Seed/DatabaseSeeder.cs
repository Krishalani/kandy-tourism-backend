using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Infrastructure.Seed;

public sealed record SeedResult(int CategoriesAdded, int StartPresetsAdded, int AttractionsAdded);

/// <summary>
/// Inserts any <see cref="SeedData"/> record whose slug is not in the database yet. Existing rows
/// (including admin edits and deactivated attractions) are never updated, so it is safe on every start.
/// </summary>
public sealed class DatabaseSeeder(KandyTourismDbContext db, TimeProvider clock, ILogger<DatabaseSeeder> logger)
{
    public async Task<SeedResult> SeedAsync(CancellationToken ct = default)
    {
        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug, ct);
        var categoriesAdded = 0;
        foreach (var seed in SeedData.Categories.Where(c => !categories.ContainsKey(c.Slug)))
        {
            var category = new AttractionCategory { Slug = seed.Slug, Name = seed.Name, DisplayOrder = seed.DisplayOrder };
            db.Categories.Add(category);
            categories[seed.Slug] = category;
            categoriesAdded++;
        }

        var presetSlugs = (await db.StartPresets.Select(p => p.Slug).ToListAsync(ct)).ToHashSet();
        var presetsAdded = 0;
        foreach (var seed in SeedData.StartPresets.Where(p => !presetSlugs.Contains(p.Slug)))
        {
            db.StartPresets.Add(new StartPreset
            {
                Slug = seed.Slug,
                Label = seed.Label,
                Description = seed.Description,
                Latitude = seed.Latitude,
                Longitude = seed.Longitude,
                CoordinatesApproximate = seed.CoordinatesApproximate,
                IsDefault = seed.IsDefault,
                DisplayOrder = seed.DisplayOrder,
            });
            presetsAdded++;
        }
        await db.SaveChangesAsync(ct);

        var attractionSlugs = (await db.Attractions.Select(a => a.Slug).ToListAsync(ct)).ToHashSet();
        var now = clock.GetUtcNow().UtcDateTime;
        var attractionsAdded = 0;
        foreach (var seed in SeedData.Attractions.Where(a => !attractionSlugs.Contains(a.Slug)))
        {
            db.Attractions.Add(ToEntity(seed, categories[seed.Category], now));
            // One at a time so identity values follow catalogue order (used by the "recommended" sort).
            await db.SaveChangesAsync(ct);
            attractionsAdded++;
        }

        var result = new SeedResult(categoriesAdded, presetsAdded, attractionsAdded);
        if (categoriesAdded + presetsAdded + attractionsAdded > 0)
        {
            logger.LogInformation(
                "Seeded {Categories} categories, {Presets} start points and {Attractions} attractions.",
                categoriesAdded, presetsAdded, attractionsAdded);
        }
        return result;
    }

    public static Attraction ToEntity(SeedAttraction seed, AttractionCategory category, DateTime now) => new()
    {
        Name = seed.Name,
        Slug = seed.Slug,
        Category = category,
        ShortDescription = seed.ShortDescription,
        Description = [.. seed.Description],
        ImageUrl = CommonsImage.Url(seed.ImagePath),
        ImageAlt = seed.ImageAlt,
        ImageCreditLabel = CommonsImage.CreditLabel,
        ImageCreditSourceUrl = CommonsImage.SourceUrl(seed.ImagePath),
        Gallery = seed.Gallery
            .Select((g, i) => new AttractionImage
            {
                Url = CommonsImage.Url(g.CommonsPath),
                AltText = g.Alt,
                CreditLabel = CommonsImage.CreditLabel,
                CreditSourceUrl = CommonsImage.SourceUrl(g.CommonsPath),
                SortOrder = i + 1,
            })
            .ToList(),
        Address = seed.Address,
        Area = seed.Area,
        Latitude = seed.Latitude,
        Longitude = seed.Longitude,
        CoordinatesApproximate = seed.CoordinatesApproximate,
        EstimatedVisitDurationMinutes = seed.EstimatedVisitDurationMinutes,
        OpeningHours = seed.OpeningHours,
        EntranceFee = seed.EntranceFee,
        VisitorTips = [.. seed.VisitorTips],
        Tags = [.. seed.Tags],
        SearchText = SearchTextNormalizer.BuildAttractionSearchText(seed.Name, seed.Area, category.Name, seed.Tags),
        IsFeatured = seed.Featured,
        IsActive = true,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
    };
}
