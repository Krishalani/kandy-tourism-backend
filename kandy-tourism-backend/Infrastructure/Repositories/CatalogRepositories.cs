using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Enums;
using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Infrastructure.Repositories;

public sealed class CategoryRepository(KandyTourismDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<AttractionCategory>> GetActiveAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    /// <remarks>Tracked, so it can be assigned to an attraction's navigation property.</remarks>
    public Task<AttractionCategory?> GetActiveBySlugAsync(string slug, CancellationToken ct) =>
        db.Categories.FirstOrDefaultAsync(c => c.IsActive && c.Slug == slug, ct);
}

public sealed class StartPresetRepository(KandyTourismDbContext db) : IStartPresetRepository
{
    public async Task<IReadOnlyList<StartPreset>> GetActiveAsync(CancellationToken ct) =>
        await db.StartPresets.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Label)
            .ToListAsync(ct);

    public Task<StartPreset?> GetActiveBySlugAsync(string slug, CancellationToken ct) =>
        db.StartPresets.FirstOrDefaultAsync(p => p.IsActive && p.Slug == slug, ct);

    public Task<StartPreset?> GetDefaultAsync(CancellationToken ct) =>
        db.StartPresets
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .FirstOrDefaultAsync(ct);
}

public sealed class ItineraryRepository(KandyTourismDbContext db) : IItineraryRepository
{
    public Task<Itinerary?> GetByPublicIdAsync(Guid publicId, CancellationToken ct) =>
        db.Itineraries
            .Include(i => i.Stops).ThenInclude(s => s.Attraction).ThenInclude(a => a.Category)
            .Include(i => i.StartPreset)
            .Include(i => i.StartAttraction)
            .Include(i => i.Feedback).ThenInclude(f => f!.StopRatings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.PublicId == publicId, ct);

    public Task<Itinerary?> GetByShareIdAsync(Guid shareId, CancellationToken ct) =>
        db.Itineraries.AsNoTracking()
            .Include(i => i.Stops).ThenInclude(s => s.Attraction).ThenInclude(a => a.Category)
            .Include(i => i.StartPreset)
            .Include(i => i.StartAttraction)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.ShareId == shareId, ct);

    public async Task<(int Itineraries, int ActiveStops)> GetCountsAsync(CancellationToken ct) =>
        (await db.Itineraries.CountAsync(ct),
         await db.ItineraryStops.CountAsync(s => s.Attraction.IsActive, ct));

    public async Task<TripFeedbackStatistics> GetFeedbackStatisticsAsync(int recentCount, int topCount, CancellationToken ct)
    {
        var confirmed = await db.Itineraries.CountAsync(i => i.Status == ItineraryStatus.Confirmed, ct);
        var completed = await db.Itineraries.CountAsync(i => i.Status == ItineraryStatus.Completed, ct);
        var average = await db.TripFeedback.Select(f => (double?)f.Rating).AverageAsync(ct);

        var recent = await db.TripFeedback.AsNoTracking()
            .OrderByDescending(f => f.SubmittedAtUtc).ThenByDescending(f => f.Id)
            .Take(recentCount)
            .Select(f => new
            {
                f.Rating,
                f.Comment,
                f.Itinerary.TripDate,
                StopCount = f.Itinerary.Stops.Count(s => s.Attraction.IsActive),
                f.SubmittedAtUtc,
            })
            .ToListAsync(ct);

        var topRated = await db.TripStopRatings.AsNoTracking()
            .Where(r => r.Attraction.IsActive)
            .GroupBy(r => new { r.Attraction.Slug, r.Attraction.Name })
            .Select(g => new { g.Key.Slug, g.Key.Name, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .OrderByDescending(a => a.Average).ThenByDescending(a => a.Count).ThenBy(a => a.Name)
            .Take(topCount)
            .ToListAsync(ct);

        return new TripFeedbackStatistics(
            confirmed,
            completed,
            average,
            recent.Select(f => new RecentTripFeedback(f.Rating, f.Comment, f.TripDate, f.StopCount, f.SubmittedAtUtc)).ToList(),
            topRated.Select(a => new AttractionRating(a.Slug, a.Name, a.Average, a.Count)).ToList());
    }

    public void Add(Itinerary itinerary) => db.Itineraries.Add(itinerary);

    public void RemoveStop(ItineraryStop stop) => db.ItineraryStops.Remove(stop);
}

public sealed class AdminUserRepository(KandyTourismDbContext db) : IAdminUserRepository
{
    public Task<AdminUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.AdminUsers.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<AdminUser?> GetByIdAsync(int id, CancellationToken ct) =>
        db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);

    public void Add(AdminUser user) => db.AdminUsers.Add(user);
}
