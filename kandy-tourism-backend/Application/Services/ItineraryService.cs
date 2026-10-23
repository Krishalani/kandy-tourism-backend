using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Application.Mapping;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Enums;
using KandyTourism.Api.Domain.Exceptions;

namespace KandyTourism.Api.Application.Services;

/// <summary>
/// Guest itinerary use cases. Stop order is persisted as a 1-based <see cref="ItineraryStop.Sequence"/>
/// that is unique per itinerary; every multi-row change runs in a transaction.
/// </summary>
/// <remarks>
/// Stops whose attraction an admin has since deleted (deactivated) are kept in the database but are
/// not part of the visible plan: they are excluded from responses, limits and reorder validation.
/// </remarks>
public sealed class ItineraryService(
    IItineraryRepository itineraries,
    IAttractionRepository attractions,
    IStartPresetRepository presets,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IItineraryService
{
    /// <summary>Trip dates are Sri Lanka calendar days (UTC+05:30, no daylight saving).</summary>
    private static readonly TimeSpan SriLankaUtcOffset = TimeSpan.FromMinutes(330);
    private const int MaxTripDaysAhead = 365;

    public async Task<CreatedItineraryDto> CreateAsync(CancellationToken ct)
    {
        var preset = await presets.GetDefaultAsync(ct)
            ?? throw new InvalidOperationException("No active start presets exist. Seed the database before creating itineraries.");

        var token = GuestAccessToken.Generate();
        var now = Now();
        var itinerary = new Itinerary
        {
            PublicId = Guid.NewGuid(),
            AccessTokenHash = GuestAccessToken.Hash(token),
            StartType = StartPointType.Preset,
            StartPresetId = preset.Id,
            StartPreset = preset,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        itineraries.Add(itinerary);
        await unitOfWork.SaveChangesAsync(ct);
        return new CreatedItineraryDto(token, await ToDtoAsync(itinerary, ct));
    }

    public async Task<ItineraryDto> GetAsync(Guid id, string? accessToken, CancellationToken ct) =>
        await ToDtoAsync(await LoadAsync(id, accessToken, ct), ct);

    public async Task<ItineraryDto> UpdateStartAsync(Guid id, string? accessToken, UpdateStartRequest request, CancellationToken ct)
    {
        var itinerary = await LoadAsync(id, accessToken, ct);

        switch (request.Type)
        {
            case StartPointType.Preset:
                var preset = string.IsNullOrWhiteSpace(request.PresetSlug)
                    ? null
                    : await presets.GetActiveBySlugAsync(request.PresetSlug.Trim().ToLowerInvariant(), ct);
                if (preset is null)
                {
                    throw InvalidStart("presetSlug", "Choose one of the listed starting points.");
                }
                SetStart(itinerary, StartPointType.Preset, preset: preset);
                break;

            case StartPointType.Attraction:
                var attraction = request.AttractionId is { } attractionId
                    ? await attractions.GetActiveByIdAsync(attractionId, ct)
                    : null;
                if (attraction is null)
                {
                    throw InvalidStart("attractionId", "Choose an attraction that is currently listed.");
                }
                SetStart(itinerary, StartPointType.Attraction, attraction: attraction);
                break;

            case StartPointType.Custom:
                var query = request.Query?.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    throw InvalidStart("query", "Type an address or place name.");
                }
                SetStart(itinerary, StartPointType.Custom, query: query);
                break;

            default:
                throw InvalidStart("type", "Choose a starting point type: preset, attraction or custom.");
        }

        itinerary.UpdatedAtUtc = Now();
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync(itinerary, ct);
    }

    public Task<ItineraryDto> AddStopAsync(Guid id, string? accessToken, AddStopRequest request, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var itinerary = await LoadAsync(id, accessToken, token);
            var visible = VisibleStops(itinerary);

            if (visible.Any(s => s.AttractionId == request.AttractionId))
            {
                throw new ConflictException("duplicate-stop", "This attraction is already in your plan.");
            }
            if (visible.Count >= Itinerary.MaxStops)
            {
                throw new ConflictException("itinerary-full",
                    $"A plan can hold up to {Itinerary.MaxStops} stops, the most a Google Maps link supports. Remove one to add another.");
            }

            var attraction = itinerary.Stops.Any(s => s.AttractionId == request.AttractionId)
                ? null
                : await attractions.GetActiveByIdAsync(request.AttractionId, token);
            if (attraction is null)
            {
                throw new BusinessRuleException("attraction-unavailable", "That attraction isn't available to add to a plan.");
            }

            var position = Math.Clamp(request.Position ?? visible.Count, 0, visible.Count);
            visible.Insert(position, NewStop(attraction));
            await ApplyOrderAsync(itinerary, visible, token);
            return await ToDtoAsync(itinerary, token);
        }, ct);

    public Task<ItineraryDto> AddStopsBySlugAsync(Guid id, string? accessToken, AddStopsBySlugRequest request, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var itinerary = await LoadAsync(id, accessToken, token);
            var visible = VisibleStops(itinerary);

            var slugs = request.AttractionSlugs
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var found = (await attractions.GetActiveBySlugsAsync(slugs, token)).ToDictionary(a => a.Slug);

            var added = false;
            foreach (var slug in slugs)
            {
                if (visible.Count >= Itinerary.MaxStops)
                {
                    break;
                }
                if (found.TryGetValue(slug, out var attraction) && itinerary.Stops.All(s => s.AttractionId != attraction.Id))
                {
                    visible.Add(NewStop(attraction));
                    added = true;
                }
            }

            if (added)
            {
                await ApplyOrderAsync(itinerary, visible, token);
            }
            return await ToDtoAsync(itinerary, token);
        }, ct);

    public Task<ItineraryDto> RemoveStopAsync(Guid id, string? accessToken, int attractionId, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var itinerary = await LoadAsync(id, accessToken, token);
            var stop = itinerary.Stops.FirstOrDefault(s => s.AttractionId == attractionId)
                ?? throw new NotFoundException("stop-not-found", "That attraction isn't in your plan.");

            itinerary.Stops.Remove(stop);
            itineraries.RemoveStop(stop);
            await unitOfWork.SaveChangesAsync(token);

            await ApplyOrderAsync(itinerary, VisibleStops(itinerary), token);
            return await ToDtoAsync(itinerary, token);
        }, ct);

    public Task<ItineraryDto> ReorderStopsAsync(Guid id, string? accessToken, ReorderStopsRequest request, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var itinerary = await LoadAsync(id, accessToken, token);
            var visible = VisibleStops(itinerary);
            var requested = request.AttractionIds;

            var isExactPermutation =
                requested.Count == visible.Count &&
                requested.Distinct().Count() == requested.Count &&
                requested.ToHashSet().SetEquals(visible.Select(s => s.AttractionId));
            if (!isExactPermutation)
            {
                throw new BusinessRuleException("invalid-order",
                    "The new order must list every stop in your plan exactly once. Refresh the page and try again.");
            }

            var byAttraction = visible.ToDictionary(s => s.AttractionId);
            await ApplyOrderAsync(itinerary, requested.Select(a => byAttraction[a]).ToList(), token);
            return await ToDtoAsync(itinerary, token);
        }, ct);

    public Task<ItineraryDto> ClearStopsAsync(Guid id, string? accessToken, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var itinerary = await LoadAsync(id, accessToken, token);
            foreach (var stop in itinerary.Stops.ToList())
            {
                itinerary.Stops.Remove(stop);
                itineraries.RemoveStop(stop);
            }
            itinerary.UpdatedAtUtc = Now();
            await unitOfWork.SaveChangesAsync(token);
            return await ToDtoAsync(itinerary, token);
        }, ct);

    public async Task<ItineraryDto> UpdateTripDateAsync(Guid id, string? accessToken, UpdateTripDateRequest request,
        CancellationToken ct)
    {
        var itinerary = await LoadAsync(id, accessToken, ct);
        if (request.TripDate is { } tripDate)
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(SriLankaUtcOffset).DateTime);
            if (tripDate < today)
            {
                throw InvalidTripDate("Choose today or a later date.");
            }
            if (tripDate > today.AddDays(MaxTripDaysAhead))
            {
                throw InvalidTripDate("Choose a date within the next year.");
            }
        }

        itinerary.TripDate = request.TripDate;
        itinerary.UpdatedAtUtc = Now();
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync(itinerary, ct);
    }

    public async Task<ItineraryDto> EnableSharingAsync(Guid id, string? accessToken, CancellationToken ct)
    {
        var itinerary = await LoadAsync(id, accessToken, ct);
        if (itinerary.ShareId is null)
        {
            itinerary.ShareId = Guid.NewGuid();
            await unitOfWork.SaveChangesAsync(ct);
        }
        return await ToDtoAsync(itinerary, ct);
    }

    public async Task<ItineraryDto> DisableSharingAsync(Guid id, string? accessToken, CancellationToken ct)
    {
        var itinerary = await LoadAsync(id, accessToken, ct);
        if (itinerary.ShareId is not null)
        {
            itinerary.ShareId = null;
            await unitOfWork.SaveChangesAsync(ct);
        }
        return await ToDtoAsync(itinerary, ct);
    }

    public async Task<SharedItineraryDto> GetSharedAsync(Guid shareId, CancellationToken ct)
    {
        var itinerary = await itineraries.GetByShareIdAsync(shareId, ct)
            ?? throw new NotFoundException("shared-itinerary-not-found",
                "This shared plan is no longer available. Its owner may have stopped sharing it.");
        var plan = await ToDtoAsync(itinerary, ct);
        return new SharedItineraryDto
        {
            ShareId = shareId,
            Start = plan.Start,
            Stops = plan.Stops,
            TripDate = plan.TripDate,
            UpdatedAtUtc = plan.UpdatedAtUtc,
        };
    }

    private async Task<Itinerary> LoadAsync(Guid id, string? accessToken, CancellationToken ct)
    {
        if (!GuestAccessToken.IsWellFormed(accessToken))
        {
            throw new UnauthorizedException("itinerary-token-required",
                "This plan can only be opened with its access token (X-Itinerary-Token header).");
        }

        var itinerary = await itineraries.GetByPublicIdAsync(id, ct);
        // The same response for "missing" and "wrong token" avoids revealing which plans exist.
        if (itinerary is null || !GuestAccessToken.Matches(accessToken!, itinerary.AccessTokenHash))
        {
            throw new NotFoundException("itinerary-not-found", "We couldn't find that saved plan.");
        }
        return itinerary;
    }

    private static List<ItineraryStop> VisibleStops(Itinerary itinerary) =>
        itinerary.Stops.Where(s => s.Attraction.IsActive).OrderBy(s => s.Sequence).ToList();

    private ItineraryStop NewStop(Attraction attraction) =>
        new() { Attraction = attraction, AttractionId = attraction.Id, CreatedAtUtc = Now() };

    /// <summary>
    /// Persists <paramref name="orderedVisible"/> as sequence 1..n (hidden stops follow). Existing rows are
    /// first moved to a temporary range above every final value so the unique (ItineraryId, Sequence)
    /// index is never violated part-way through, regardless of statement order.
    /// </summary>
    private async Task ApplyOrderAsync(Itinerary itinerary, IReadOnlyList<ItineraryStop> orderedVisible, CancellationToken ct)
    {
        var hidden = itinerary.Stops.Where(s => !s.Attraction.IsActive).OrderBy(s => s.Sequence);
        var final = orderedVisible.Concat(hidden).ToList();
        var persisted = final.Where(s => s.Id != 0).ToList();

        if (persisted.Any(s => s.Sequence != final.IndexOf(s) + 1))
        {
            var offset = Math.Max(final.Count, itinerary.Stops.Count == 0 ? 0 : itinerary.Stops.Max(s => s.Sequence));
            for (var i = 0; i < persisted.Count; i++)
            {
                persisted[i].Sequence = offset + i + 1;
            }
            await unitOfWork.SaveChangesAsync(ct);
        }

        for (var i = 0; i < final.Count; i++)
        {
            final[i].Sequence = i + 1;
            if (final[i].Id == 0)
            {
                itinerary.Stops.Add(final[i]);
            }
        }
        itinerary.UpdatedAtUtc = Now();
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static void SetStart(Itinerary itinerary, StartPointType type,
        StartPreset? preset = null, Attraction? attraction = null, string? query = null)
    {
        itinerary.StartType = type;
        itinerary.StartPreset = preset;
        itinerary.StartPresetId = preset?.Id;
        itinerary.StartAttraction = attraction;
        itinerary.StartAttractionId = attraction?.Id;
        itinerary.StartQuery = query;
    }

    private async Task<ItineraryDto> ToDtoAsync(Itinerary itinerary, CancellationToken ct) => new()
    {
        Id = itinerary.PublicId,
        Start = await ResolveStartAsync(itinerary, ct),
        Stops = VisibleStops(itinerary)
            .Select((s, i) => new ItineraryStopDto(i + 1, s.Attraction.ToSummaryDto()))
            .ToList(),
        MaxStops = Itinerary.MaxStops,
        TripDate = itinerary.TripDate,
        ShareId = itinerary.ShareId,
        UpdatedAtUtc = itinerary.UpdatedAtUtc,
    };

    private async Task<ItineraryStartDto> ResolveStartAsync(Itinerary itinerary, CancellationToken ct)
    {
        switch (itinerary.StartType)
        {
            case StartPointType.Preset when itinerary.StartPreset is { IsActive: true } preset:
                return PresetStart(preset);
            case StartPointType.Attraction when itinerary.StartAttraction is { IsActive: true } a:
                return new ItineraryStartDto
                {
                    Type = StartPointType.Attraction,
                    AttractionId = a.Id,
                    Label = a.Name,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    CoordinatesApproximate = a.CoordinatesApproximate,
                };
            case StartPointType.Custom when !string.IsNullOrEmpty(itinerary.StartQuery):
                return new ItineraryStartDto
                {
                    Type = StartPointType.Custom,
                    Query = itinerary.StartQuery,
                    Label = itinerary.StartQuery,
                };
        }

        // The chosen start is no longer available (e.g. the attraction was removed): fall back to the default.
        var fallback = await presets.GetDefaultAsync(ct);
        return fallback is null
            ? new ItineraryStartDto { Type = StartPointType.Custom, Label = "Starting point not set" }
            : PresetStart(fallback);
    }

    private static ItineraryStartDto PresetStart(StartPreset preset) => new()
    {
        Type = StartPointType.Preset,
        PresetSlug = preset.Slug,
        Label = preset.Label,
        Latitude = preset.Latitude,
        Longitude = preset.Longitude,
        CoordinatesApproximate = preset.CoordinatesApproximate,
    };

    private static BusinessRuleException InvalidStart(string field, string message) =>
        new("invalid-start", message, new Dictionary<string, string[]> { [field] = [message] });

    private static BusinessRuleException InvalidTripDate(string message) =>
        new("invalid-trip-date", message, new Dictionary<string, string[]> { ["tripDate"] = [message] });

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
