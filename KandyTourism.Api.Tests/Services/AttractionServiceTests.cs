using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Domain.Exceptions;
using KandyTourism.Api.Tests.Infrastructure;

namespace KandyTourism.Api.Tests.Services;

public sealed class AttractionServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<IReadOnlyList<string>> SearchSlugs(AttractionListQuery query)
    {
        await using var db = _database.CreateContext();
        var result = await _database.CreateAttractionService(db).SearchAsync(query, CancellationToken.None);
        return result.Items.Select(i => i.Slug).ToList();
    }

    [Fact]
    public async Task Search_matches_name_area_category_and_tags_but_not_descriptions()
    {
        // Many descriptions mention "the Temple of the Tooth"; only genuinely temple-related places should match.
        var slugs = await SearchSlugs(new AttractionListQuery { Q = "temple" });

        Assert.Equal(
            ["temple-of-the-sacred-tooth-relic", "bahirawakanda-buddha-statue", "degaldoruwa-raja-maha-vihara",
             "lankatilaka-vihara", "gadaladeniya-vihara"],
            slugs);
    }

    [Fact]
    public async Task Search_ignores_case_accents_and_apostrophes()
    {
        Assert.Equal(["st-pauls-church-kandy"], await SearchSlugs(new AttractionListQuery { Q = "PAULS church" }));
        Assert.Contains("kandy-view-point", await SearchSlugs(new AttractionListQuery { Q = "arthurs seat" }));
    }

    [Fact]
    public async Task Filters_by_category_duration_and_featured()
    {
        var religiousShort = await SearchSlugs(new AttractionListQuery { Category = "religious", Duration = "short" });
        Assert.Equal(["gadaladeniya-vihara"], religiousShort);

        var longVisits = await SearchSlugs(new AttractionListQuery { Duration = "long", Sort = "duration-desc" });
        Assert.Equal(["hanthana-mountain-range", "royal-botanical-gardens-peradeniya"], longVisits);

        var featured = await SearchSlugs(new AttractionListQuery { Featured = true });
        Assert.Equal(6, featured.Count);
    }

    [Fact]
    public async Task Recommended_sort_lists_featured_first_and_pages_consistently()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateAttractionService(db);

        var page1 = await service.SearchAsync(new AttractionListQuery { PageSize = 10 }, CancellationToken.None);
        var page2 = await service.SearchAsync(new AttractionListQuery { PageSize = 10, Page = 2 }, CancellationToken.None);

        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.True(page1.HasNextPage);
        Assert.All(page1.Items.Take(6), a => Assert.True(a.Featured));
        Assert.Equal(5, page2.Items.Count);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task Rejects_sort_values_outside_the_allowlist()
    {
        await using var db = _database.CreateContext();
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _database.CreateAttractionService(db).SearchAsync(new AttractionListQuery { Sort = "Name; DROP TABLE" }, CancellationToken.None));
        Assert.Equal("invalid-sort", error.Code);
    }

    [Fact]
    public async Task Create_rejects_unknown_category_and_duplicate_slug()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateAttractionService(db);

        var badCategory = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(NewAttraction() with { Category = "beaches" }, CancellationToken.None));
        Assert.Equal("invalid-category", badCategory.Code);

        var duplicate = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(NewAttraction() with { Slug = "kandy-lake" }, CancellationToken.None));
        Assert.Equal("slug-taken", duplicate.Code);
        Assert.Contains("slug", duplicate.Errors!.Keys);
    }

    [Fact]
    public async Task Create_then_search_finds_it_by_tag_and_delete_hides_it()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateAttractionService(db);

        var created = await service.CreateAsync(NewAttraction(), CancellationToken.None);
        Assert.Equal("nature", created.Category);
        Assert.Equal(["One", "Two"], created.Description);
        Assert.Single(created.Gallery);
        Assert.Contains("orchid-test-garden", await SearchSlugs(new AttractionListQuery { Q = "butterflies" }));

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.DoesNotContain("orchid-test-garden", await SearchSlugs(new AttractionListQuery { PageSize = 100 }));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id, CancellationToken.None));

        // The slug can be reused once the original has been deleted (deactivated).
        var replacement = await service.CreateAsync(NewAttraction(), CancellationToken.None);
        Assert.NotEqual(created.Id, replacement.Id);
    }

    [Fact]
    public async Task Nearby_returns_closest_other_attractions_in_distance_order()
    {
        var templeId = _database.AttractionId("temple-of-the-sacred-tooth-relic");
        await using var db = _database.CreateContext();

        var nearby = await _database.CreateAttractionService(db).GetNearbyAsync(templeId, 3, CancellationToken.None);

        Assert.Equal(3, nearby.Count);
        Assert.DoesNotContain(nearby, n => n.Attraction.Id == templeId);
        Assert.Equal(nearby.OrderBy(n => n.DistanceKm).Select(n => n.Attraction.Id), nearby.Select(n => n.Attraction.Id));
        Assert.All(nearby, n => Assert.InRange(n.DistanceKm, 0, 1));
    }

    private static AttractionUpsertRequest NewAttraction() => new()
    {
        Name = "Orchid Test Garden",
        Slug = "orchid-test-garden",
        Category = "nature",
        ShortDescription = "A test attraction.",
        Description = [" One ", "", "Two"],
        ImageUrl = "/images/attraction-placeholder.svg",
        ImageAlt = "Placeholder",
        Gallery = [new GalleryImageDto { Url = "https://example.org/a.jpg", Alt = "A" }],
        Address = "Test Road, Kandy",
        Area = "Kandy City",
        Latitude = 7.29,
        Longitude = 80.63,
        EstimatedVisitDurationMinutes = 40,
        Tags = ["butterflies", "Butterflies", "garden"],
    };
}
