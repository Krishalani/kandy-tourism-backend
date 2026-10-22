using System.ComponentModel.DataAnnotations;

namespace KandyTourism.Api.Application.DTOs;

public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}

public sealed record AdminProfileDto(int Id, string Email, string DisplayName, string Role);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, AdminProfileDto Admin);

public sealed record CategoryCountDto(string Slug, string Name, int Count);

public sealed record AdminDashboardDto(
    int TotalAttractions,
    int FeaturedAttractions,
    int CategoriesInUse,
    int ApproximateCoordinates,
    IReadOnlyList<CategoryCountDto> ByCategory,
    int GuestItineraries,
    int PlannedStops);
