namespace KandyTourism.Api.Application.DTOs;

public sealed record CategoryDto(string Slug, string Name, string? Description);

public sealed record StartPointDto(
    string Slug,
    string Label,
    string Description,
    double Latitude,
    double Longitude,
    bool CoordinatesApproximate,
    bool IsDefault);
