using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;

namespace KandyTourism.Api.Application.Services;

public sealed class AdminDashboardService(
    IAttractionRepository attractions,
    ICategoryRepository categories,
    IItineraryRepository itineraries) : IAdminDashboardService
{
    public async Task<AdminDashboardDto> GetAsync(CancellationToken ct)
    {
        var stats = await attractions.GetStatisticsAsync(ct);
        var byCategory = (await categories.GetActiveAsync(ct))
            .Select(c => new CategoryCountDto(c.Slug, c.Name, stats.CountByCategoryId.GetValueOrDefault(c.Id)))
            .ToList();
        var (guestItineraries, plannedStops) = await itineraries.GetCountsAsync(ct);

        return new AdminDashboardDto(
            stats.Total,
            stats.Featured,
            byCategory.Count(c => c.Count > 0),
            stats.ApproximateCoordinates,
            byCategory,
            guestItineraries,
            plannedStops);
    }
}
