using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Domain.Entities;

namespace KandyTourism.Api.Application.Interfaces;

public interface IAttractionRepository
{
    /// <summary>Active attractions matching the criteria, with categories loaded (read-only).</summary>
    Task<(IReadOnlyList<Attraction> Items, int TotalCount)> SearchAsync(AttractionSearchCriteria criteria, CancellationToken ct);

    /// <summary>Read-only details (category and gallery loaded).</summary>
    Task<Attraction?> GetDetailsByIdAsync(int id, bool includeInactive, CancellationToken ct);

    Task<Attraction?> GetActiveDetailsBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Tracked entity with gallery loaded, for updates.</summary>
    Task<Attraction?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>Tracked active attraction (with category) for adding to an itinerary.</summary>
    Task<Attraction?> GetActiveByIdAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<Attraction>> GetActiveBySlugsAsync(IReadOnlyCollection<string> slugs, CancellationToken ct);

    /// <summary>Whether an active attraction other than <paramref name="exceptId"/> uses the slug.</summary>
    Task<bool> ActiveSlugExistsAsync(string slug, int? exceptId, CancellationToken ct);

    /// <summary>Nearest active attractions, ordered by approximate distance computed in the database.</summary>
    Task<IReadOnlyList<Attraction>> GetNearestAsync(int originId, double latitude, double longitude, int limit, CancellationToken ct);

    Task<AttractionStatistics> GetStatisticsAsync(CancellationToken ct);

    void Add(Attraction attraction);
}

public sealed record AttractionStatistics(
    int Total,
    int Featured,
    int ApproximateCoordinates,
    IReadOnlyDictionary<int, int> CountByCategoryId);

public interface ICategoryRepository
{
    Task<IReadOnlyList<AttractionCategory>> GetActiveAsync(CancellationToken ct);
    Task<AttractionCategory?> GetActiveBySlugAsync(string slug, CancellationToken ct);
}

public interface IStartPresetRepository
{
    Task<IReadOnlyList<StartPreset>> GetActiveAsync(CancellationToken ct);
    Task<StartPreset?> GetActiveBySlugAsync(string slug, CancellationToken ct);
    /// <summary>The preset flagged as default, otherwise the first active one.</summary>
    Task<StartPreset?> GetDefaultAsync(CancellationToken ct);
}

public interface IItineraryRepository
{
    /// <summary>Tracked itinerary with stops (and their attractions/categories) and start references loaded.</summary>
    Task<Itinerary?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<(int Itineraries, int ActiveStops)> GetCountsAsync(CancellationToken ct);
    void Add(Itinerary itinerary);
    void RemoveStop(ItineraryStop stop);
}

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<AdminUser?> GetByIdAsync(int id, CancellationToken ct);
    void Add(AdminUser user);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Runs the operation in a database transaction, committing only if it completes.</summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
