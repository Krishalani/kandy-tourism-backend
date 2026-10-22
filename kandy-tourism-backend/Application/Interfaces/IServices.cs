using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Domain.Entities;

namespace KandyTourism.Api.Application.Interfaces;

public interface IAttractionService
{
    Task<PagedResult<AttractionSummaryDto>> SearchAsync(AttractionListQuery query, CancellationToken ct);
    Task<AttractionDetailsDto> GetByIdAsync(int id, CancellationToken ct);
    Task<AttractionDetailsDto> GetBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<NearbyAttractionDto>> GetNearbyAsync(int id, int limit, CancellationToken ct);

    Task<AttractionDetailsDto> GetForAdminAsync(int id, CancellationToken ct);
    Task<AttractionDetailsDto> CreateAsync(AttractionUpsertRequest request, CancellationToken ct);
    Task<AttractionDetailsDto> UpdateAsync(int id, AttractionUpsertRequest request, CancellationToken ct);
    /// <summary>Deactivates the attraction; it disappears from listings but itinerary rows are kept.</summary>
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<StartPointDto>> GetStartPointsAsync(CancellationToken ct);
}

public interface IItineraryService
{
    Task<CreatedItineraryDto> CreateAsync(CancellationToken ct);
    Task<ItineraryDto> GetAsync(Guid id, string? accessToken, CancellationToken ct);
    Task<ItineraryDto> UpdateStartAsync(Guid id, string? accessToken, UpdateStartRequest request, CancellationToken ct);
    Task<ItineraryDto> AddStopAsync(Guid id, string? accessToken, AddStopRequest request, CancellationToken ct);
    Task<ItineraryDto> AddStopsBySlugAsync(Guid id, string? accessToken, AddStopsBySlugRequest request, CancellationToken ct);
    Task<ItineraryDto> RemoveStopAsync(Guid id, string? accessToken, int attractionId, CancellationToken ct);
    Task<ItineraryDto> ReorderStopsAsync(Guid id, string? accessToken, ReorderStopsRequest request, CancellationToken ct);
    Task<ItineraryDto> ClearStopsAsync(Guid id, string? accessToken, CancellationToken ct);
}

public interface IAdminAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AdminProfileDto> GetProfileAsync(int adminId, CancellationToken ct);
}

public interface IAdminUserService
{
    /// <summary>Creates an active admin account with a hashed password.</summary>
    Task<AdminProfileDto> CreateAsync(CreateAdminRequest request, CancellationToken ct);
}

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken ct);
}

public interface IAccessTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AdminUser user);
}
