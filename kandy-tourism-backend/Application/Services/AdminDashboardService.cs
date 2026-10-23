using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;

namespace KandyTourism.Api.Application.Services;

public sealed class AdminDashboardService(
    IAttractionRepository attractions,
    ICategoryRepository categories,
    IItineraryRepository itineraries,
    IContactRepository contacts) : IAdminDashboardService
{
    private const int RecentMessageCount = 5;
    private const int RecentFeedbackCount = 5;
    private const int TopRatedCount = 5;

    public async Task<AdminDashboardDto> GetAsync(CancellationToken ct)
    {
        var stats = await attractions.GetStatisticsAsync(ct);
        var byCategory = (await categories.GetActiveAsync(ct))
            .Select(c => new CategoryCountDto(c.Slug, c.Name, stats.CountByCategoryId.GetValueOrDefault(c.Id)))
            .ToList();
        var (guestItineraries, plannedStops) = await itineraries.GetCountsAsync(ct);
        var messages = await contacts.GetStatisticsAsync(RecentMessageCount, ct);
        var feedback = await itineraries.GetFeedbackStatisticsAsync(RecentFeedbackCount, TopRatedCount, ct);

        return new AdminDashboardDto(
            stats.Total,
            stats.Featured,
            byCategory.Count(c => c.Count > 0),
            stats.ApproximateCoordinates,
            byCategory,
            guestItineraries,
            plannedStops,
            messages.Contacts,
            messages.Messages,
            messages.Unread,
            messages.Recent
                .Select(m => new RecentContactMessageDto(
                    m.MessageId,
                    m.ContactId,
                    m.Name,
                    m.Email,
                    m.Subject,
                    DateTime.SpecifyKind(m.ReceivedAtUtc, DateTimeKind.Utc),
                    m.IsRead))
                .ToList(),
            feedback.ConfirmedTrips,
            feedback.CompletedTrips,
            feedback.AverageRating is { } average ? Math.Round(average, 1) : null,
            feedback.Recent
                .Select(f => new RecentFeedbackDto(
                    f.Rating,
                    f.Comment,
                    f.TripDate,
                    f.StopCount,
                    DateTime.SpecifyKind(f.SubmittedAtUtc, DateTimeKind.Utc)))
                .ToList(),
            feedback.TopRated
                .Select(a => new RatedAttractionDto(a.Slug, a.Name, Math.Round(a.AverageRating, 1), a.RatingCount))
                .ToList());
    }
}
