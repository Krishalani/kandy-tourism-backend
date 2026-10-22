using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
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
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.PublicId == publicId, ct);

    public async Task<(int Itineraries, int ActiveStops)> GetCountsAsync(CancellationToken ct) =>
        (await db.Itineraries.CountAsync(ct),
         await db.ItineraryStops.CountAsync(s => s.Attraction.IsActive, ct));

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
