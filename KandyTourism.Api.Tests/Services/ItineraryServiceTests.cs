using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Domain.Enums;
using KandyTourism.Api.Domain.Exceptions;
using KandyTourism.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Tests.Services;

public sealed class ItineraryServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly int _lake, _temple, _bahirawakanda, _peradeniya;

    public ItineraryServiceTests()
    {
        _lake = _database.AttractionId("kandy-lake");
        _temple = _database.AttractionId("temple-of-the-sacred-tooth-relic");
        _bahirawakanda = _database.AttractionId("bahirawakanda-buddha-statue");
        _peradeniya = _database.AttractionId("royal-botanical-gardens-peradeniya");
    }

    public void Dispose() => _database.Dispose();

    /// <summary>Each call uses a fresh DbContext, like separate HTTP requests.</summary>
    private async Task<T> Run<T>(Func<Application.Services.ItineraryService, Task<T>> action)
    {
        await using var db = _database.CreateContext();
        return await action(_database.CreateItineraryService(db));
    }

    private async Task<(Guid Id, string Token)> CreateWithStops(params int[] attractionIds)
    {
        var created = await Run(s => s.CreateAsync(CancellationToken.None));
        foreach (var id in attractionIds)
        {
            await Run(s => s.AddStopAsync(created.Itinerary.Id, created.AccessToken, new AddStopRequest { AttractionId = id }, CancellationToken.None));
        }
        return (created.Itinerary.Id, created.AccessToken);
    }

    private static int[] Order(ItineraryDto itinerary) => itinerary.Stops.Select(s => s.Attraction.Id).ToArray();

    [Fact]
    public async Task Create_returns_a_secret_token_and_default_start()
    {
        var created = await Run(s => s.CreateAsync(CancellationToken.None));

        Assert.Equal(43, created.AccessToken.Length);
        Assert.Equal(StartPointType.Preset, created.Itinerary.Start.Type);
        Assert.Equal("kandy-city-centre", created.Itinerary.Start.PresetSlug);
        Assert.Empty(created.Itinerary.Stops);

        await using var db = _database.CreateContext();
        var stored = await db.Itineraries.SingleAsync(i => i.PublicId == created.Itinerary.Id);
        Assert.DoesNotContain(created.AccessToken, Convert.ToBase64String(stored.AccessTokenHash));
    }

    [Fact]
    public async Task Adds_stops_in_order_with_attraction_details_and_coordinates()
    {
        var (id, token) = await CreateWithStops(_lake, _temple);

        var itinerary = await Run(s => s.GetAsync(id, token, CancellationToken.None));

        Assert.Equal([_lake, _temple], Order(itinerary));
        Assert.Equal([1, 2], itinerary.Stops.Select(s => s.Order));
        var first = itinerary.Stops[0].Attraction;
        Assert.Equal("Kandy Lake", first.Name);
        Assert.Equal(7.2918, first.Latitude);
        Assert.Equal("lakes", first.Category);
    }

    [Fact]
    public async Task Rejects_duplicate_stops()
    {
        var (id, token) = await CreateWithStops(_lake);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Run(s => s.AddStopAsync(id, token, new AddStopRequest { AttractionId = _lake }, CancellationToken.None)));

        Assert.Equal("duplicate-stop", error.Code);
    }

    [Fact]
    public async Task Enforces_the_ten_stop_limit()
    {
        await using var db = _database.CreateContext();
        var ids = await db.Attractions.OrderBy(a => a.Id).Select(a => a.Id).Take(11).ToListAsync();
        var (id, token) = await CreateWithStops([.. ids.Take(10)]);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Run(s => s.AddStopAsync(id, token, new AddStopRequest { AttractionId = ids[10] }, CancellationToken.None)));

        Assert.Equal("itinerary-full", error.Code);
    }

    [Fact]
    public async Task Rejects_unknown_or_inactive_attractions()
    {
        var (id, token) = await CreateWithStops();
        await using (var db = _database.CreateContext())
        {
            var museum = await db.Attractions.SingleAsync(a => a.Slug == "ceylon-tea-museum");
            museum.IsActive = false;
            await db.SaveChangesAsync();
        }

        var inactive = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Run(s => s.AddStopAsync(id, token, new AddStopRequest { AttractionId = _database.AttractionId("ceylon-tea-museum") }, CancellationToken.None)));
        var missing = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Run(s => s.AddStopAsync(id, token, new AddStopRequest { AttractionId = 99_999 }, CancellationToken.None)));

        Assert.Equal("attraction-unavailable", inactive.Code);
        Assert.Equal("attraction-unavailable", missing.Code);
    }

    [Fact]
    public async Task Reorder_persists_the_new_sequence()
    {
        var (id, token) = await CreateWithStops(_lake, _temple, _bahirawakanda, _peradeniya);

        await Run(s => s.ReorderStopsAsync(id, token,
            new ReorderStopsRequest { AttractionIds = [_peradeniya, _lake, _temple, _bahirawakanda] }, CancellationToken.None));

        var reloaded = await Run(s => s.GetAsync(id, token, CancellationToken.None));
        Assert.Equal([_peradeniya, _lake, _temple, _bahirawakanda], Order(reloaded));

        await using var db = _database.CreateContext();
        var sequences = await db.ItineraryStops.Where(s => s.Itinerary.PublicId == id)
            .OrderBy(s => s.Sequence).Select(s => s.Sequence).ToListAsync();
        Assert.Equal([1, 2, 3, 4], sequences);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    public async Task Reorder_rejects_lists_that_are_not_an_exact_permutation(string problem)
    {
        var (id, token) = await CreateWithStops(_lake, _temple, _bahirawakanda);
        List<int> requested = problem switch
        {
            "missing" => [_temple, _lake],
            "duplicate" => [_temple, _temple, _lake],
            _ => [_temple, _lake, _bahirawakanda, _peradeniya],
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Run(s => s.ReorderStopsAsync(id, token, new ReorderStopsRequest { AttractionIds = requested }, CancellationToken.None)));

        Assert.Equal("invalid-order", error.Code);
        Assert.Equal([_lake, _temple, _bahirawakanda], Order(await Run(s => s.GetAsync(id, token, CancellationToken.None))));
    }

    [Fact]
    public async Task Remove_renumbers_and_insert_at_position_restores_the_order()
    {
        var (id, token) = await CreateWithStops(_lake, _temple, _bahirawakanda);

        var afterRemove = await Run(s => s.RemoveStopAsync(id, token, _temple, CancellationToken.None));
        Assert.Equal([_lake, _bahirawakanda], Order(afterRemove));
        Assert.Equal([1, 2], afterRemove.Stops.Select(s => s.Order));

        var afterUndo = await Run(s => s.AddStopAsync(id, token, new AddStopRequest { AttractionId = _temple, Position = 1 }, CancellationToken.None));
        Assert.Equal([_lake, _temple, _bahirawakanda], Order(afterUndo));
    }

    [Fact]
    public async Task Bulk_add_by_slug_skips_duplicates_and_unknown_slugs()
    {
        var (id, token) = await CreateWithStops(_temple);

        var itinerary = await Run(s => s.AddStopsBySlugAsync(id, token, new AddStopsBySlugRequest
        {
            AttractionSlugs = ["kandy-lake", "temple-of-the-sacred-tooth-relic", "not-a-place", "kandy-lake"],
        }, CancellationToken.None));

        Assert.Equal([_temple, _lake], Order(itinerary));
    }

    [Fact]
    public async Task Updates_the_starting_point_and_validates_it()
    {
        var (id, token) = await CreateWithStops(_lake);

        var preset = await Run(s => s.UpdateStartAsync(id, token,
            new UpdateStartRequest { Type = StartPointType.Preset, PresetSlug = "kandy-railway-station" }, CancellationToken.None));
        Assert.Equal("Kandy Railway Station", preset.Start.Label);
        Assert.True(preset.Start.CoordinatesApproximate);

        var attraction = await Run(s => s.UpdateStartAsync(id, token,
            new UpdateStartRequest { Type = StartPointType.Attraction, AttractionId = _temple }, CancellationToken.None));
        Assert.Equal(_temple, attraction.Start.AttractionId);
        Assert.Equal(7.2936, attraction.Start.Latitude);

        var custom = await Run(s => s.UpdateStartAsync(id, token,
            new UpdateStartRequest { Type = StartPointType.Custom, Query = "  Queen's Hotel, Kandy " }, CancellationToken.None));
        Assert.Equal("Queen's Hotel, Kandy", custom.Start.Label);
        Assert.Null(custom.Start.Latitude);

        var reloaded = await Run(s => s.GetAsync(id, token, CancellationToken.None));
        Assert.Equal(StartPointType.Custom, reloaded.Start.Type);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(s => s.UpdateStartAsync(id, token,
            new UpdateStartRequest { Type = StartPointType.Preset, PresetSlug = "the-moon" }, CancellationToken.None)));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Run(s => s.UpdateStartAsync(id, token,
            new UpdateStartRequest { Type = StartPointType.Custom, Query = "   " }, CancellationToken.None)));
    }

    [Fact]
    public async Task Access_requires_the_matching_token()
    {
        var (id, token) = await CreateWithStops(_lake);
        var (_, otherToken) = await CreateWithStops();

        await Assert.ThrowsAsync<UnauthorizedException>(() => Run(s => s.GetAsync(id, null, CancellationToken.None)));
        await Assert.ThrowsAsync<UnauthorizedException>(() => Run(s => s.GetAsync(id, "short", CancellationToken.None)));
        await Assert.ThrowsAsync<NotFoundException>(() => Run(s => s.GetAsync(id, otherToken, CancellationToken.None)));
        await Assert.ThrowsAsync<NotFoundException>(() => Run(s => s.GetAsync(Guid.NewGuid(), token, CancellationToken.None)));
        Assert.Single((await Run(s => s.GetAsync(id, token, CancellationToken.None))).Stops);
    }

    [Fact]
    public async Task Deactivated_attractions_are_hidden_but_their_rows_are_kept()
    {
        var (id, token) = await CreateWithStops(_lake, _temple, _bahirawakanda);
        await using (var db = _database.CreateContext())
        {
            (await db.Attractions.SingleAsync(a => a.Id == _temple)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var itinerary = await Run(s => s.GetAsync(id, token, CancellationToken.None));
        Assert.Equal([_lake, _bahirawakanda], Order(itinerary));
        Assert.Equal([1, 2], itinerary.Stops.Select(s => s.Order));

        var reordered = await Run(s => s.ReorderStopsAsync(id, token,
            new ReorderStopsRequest { AttractionIds = [_bahirawakanda, _lake] }, CancellationToken.None));
        Assert.Equal([_bahirawakanda, _lake], Order(reordered));

        await using var check = _database.CreateContext();
        Assert.Equal(3, await check.ItineraryStops.CountAsync(s => s.Itinerary.PublicId == id));
    }

    [Fact]
    public async Task Clear_removes_every_stop()
    {
        var (id, token) = await CreateWithStops(_lake, _temple);

        var cleared = await Run(s => s.ClearStopsAsync(id, token, CancellationToken.None));

        Assert.Empty(cleared.Stops);
        Assert.Empty((await Run(s => s.GetAsync(id, token, CancellationToken.None))).Stops);
    }
}
