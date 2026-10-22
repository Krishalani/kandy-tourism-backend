namespace KandyTourism.Api.Domain.Enums;

/// <summary>
/// How an itinerary's starting point is defined. A device location is deliberately not a
/// persisted option: the frontend uses it for the current session only.
/// </summary>
public enum StartPointType
{
    Preset,
    Attraction,
    Custom,
}
