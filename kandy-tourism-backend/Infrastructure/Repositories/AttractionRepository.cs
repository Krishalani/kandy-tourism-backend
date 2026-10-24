using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Infrastructure.Repositories;

public sealed class AttractionRepository(KandyTourismDbContext db) : IAttractionRepository
{
    public async Task<(IReadOnlyList<Attraction> Items, int TotalCount)> SearchAsync(
        AttractionSearchCriteria criteria, CancellationToken ct)
    {
        var query = db.Attractions.AsNoTracking().Where(a => a.IsActive);

        if (criteria.CategorySlugs.Count > 0)
        {
            var categorySlugs = criteria.CategorySlugs;
            query = query.Where(a => categorySlugs.Contains(a.Category.Slug));
        }
        if (criteria.Featured is { } featured)
        {
            query = query.Where(a => a.IsFeatured == featured);
        }
        query = criteria.Duration switch
        {
            DurationFilter.Short => query.Where(a => a.EstimatedVisitDurationMinutes < 60),
            DurationFilter.Medium => query.Where(a => a.EstimatedVisitDurationMinutes >= 60 && a.EstimatedVisitDurationMinutes <= 120),
            DurationFilter.Long => query.Where(a => a.EstimatedVisitDurationMinutes > 120),
            _ => query,
        };
        foreach (var term in criteria.Terms)
        {
            query = query.Where(a => a.SearchText.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var ordered = criteria.Sort switch
        {
            AttractionSort.Name => query.OrderBy(a => a.Name),
            AttractionSort.DurationAscending => query
                .OrderBy(a => a.EstimatedVisitDurationMinutes == null)
                .ThenBy(a => a.EstimatedVisitDurationMinutes)
                .ThenBy(a => a.Name),
            AttractionSort.DurationDescending => query
                .OrderBy(a => a.EstimatedVisitDurationMinutes == null)
                .ThenByDescending(a => a.EstimatedVisitDurationMinutes)
                .ThenBy(a => a.Name),
            _ => query.OrderByDescending(a => a.IsFeatured),
        };

        var items = await ordered
            .ThenBy(a => a.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Include(a => a.Category)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Attraction?> GetDetailsByIdAsync(int id, bool includeInactive, CancellationToken ct) =>
        Details().FirstOrDefaultAsync(a => a.Id == id && (includeInactive || a.IsActive), ct);

    public Task<Attraction?> GetActiveDetailsBySlugAsync(string slug, CancellationToken ct) =>
        Details().FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive, ct);

    public Task<Attraction?> GetForUpdateAsync(int id, CancellationToken ct) =>
        db.Attractions
            .Include(a => a.Category)
            .Include(a => a.Gallery)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, ct);

    public Task<Attraction?> GetActiveByIdAsync(int id, CancellationToken ct) =>
        db.Attractions.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id && a.IsActive, ct);

    public async Task<IReadOnlyList<Attraction>> GetActiveBySlugsAsync(IReadOnlyCollection<string> slugs, CancellationToken ct) =>
        await db.Attractions
            .Include(a => a.Category)
            .Where(a => a.IsActive && slugs.Contains(a.Slug))
            .ToListAsync(ct);

    public Task<bool> ActiveSlugExistsAsync(string slug, int? exceptId, CancellationToken ct) =>
        db.Attractions.AnyAsync(a => a.IsActive && a.Slug == slug && (exceptId == null || a.Id != exceptId), ct);

    public async Task<IReadOnlyList<Attraction>> GetNearestAsync(
        int originId, double latitude, double longitude, int limit, CancellationToken ct)
    {
        // Equirectangular approximation: accurate enough to rank places within a region.
        var lonScale = Math.Cos(latitude * Math.PI / 180);
        return await db.Attractions
            .AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.IsActive && a.Id != originId)
            .OrderBy(a =>
                (a.Latitude - latitude) * (a.Latitude - latitude) +
                (a.Longitude - longitude) * lonScale * ((a.Longitude - longitude) * lonScale))
            .ThenBy(a => a.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<AttractionStatistics> GetStatisticsAsync(CancellationToken ct)
    {
        var active = db.Attractions.Where(a => a.IsActive);
        var total = await active.CountAsync(ct);
        var featured = await active.CountAsync(a => a.IsFeatured, ct);
        var approximate = await active.CountAsync(a => a.CoordinatesApproximate, ct);
        var byCategory = await active
            .GroupBy(a => a.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);
        return new AttractionStatistics(total, featured, approximate, byCategory);
    }

    public void Add(Attraction attraction) => db.Attractions.Add(attraction);

    private IQueryable<Attraction> Details() =>
        db.Attractions.AsNoTracking().Include(a => a.Category).Include(a => a.Gallery);
}
