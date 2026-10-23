namespace KandyTourism.Api.Domain.Enums;

/// <summary>
/// Where a guest's trip is in its life: being planned, confirmed (locked for the trip day), or
/// completed with the guest's feedback. Only a planning itinerary can be edited.
/// </summary>
public enum ItineraryStatus
{
    Planning,
    Confirmed,
    Completed,
}
