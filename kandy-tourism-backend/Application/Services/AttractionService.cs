using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Application.Mapping;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Exceptions;

namespace KandyTourism.Api.Application.Services;

public sealed class AttractionService(
    IAttractionRepository attractions,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IAttractionService
{
    private const int MaxNearby = 12;
    private const int MaxParagraphLength = 4000;
    private const int MaxTipLength = 300;
    private const int MaxTagLength = 50;

    public async Task<PagedResult<AttractionSummaryDto>> SearchAsync(AttractionListQuery query, CancellationToken ct)
    {
        var categorySlugs = (query.Category ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(slug => slug.ToLowerInvariant())
            .Distinct()
            .ToList();
        var criteria = new AttractionSearchCriteria(
            SearchTextNormalizer.ToTerms(query.Q),
            categorySlugs.Contains("all") ? [] : categorySlugs,
            ParseDuration(query.Duration),
            query.Featured,
            ParseSort(query.Sort),
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, AttractionListQuery.MaxPageSize));

        var (items, totalCount) = await attractions.SearchAsync(criteria, ct);
        return new PagedResult<AttractionSummaryDto>(
            items.Select(a => a.ToSummaryDto()).ToList(), criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<AttractionDetailsDto> GetByIdAsync(int id, CancellationToken ct) =>
        (await attractions.GetDetailsByIdAsync(id, includeInactive: false, ct) ?? throw NotFound()).ToDetailsDto();

    public async Task<AttractionDetailsDto> GetBySlugAsync(string slug, CancellationToken ct) =>
        (await attractions.GetActiveDetailsBySlugAsync(slug.Trim().ToLowerInvariant(), ct) ?? throw NotFound()).ToDetailsDto();

    public async Task<IReadOnlyList<NearbyAttractionDto>> GetNearbyAsync(int id, int limit, CancellationToken ct)
    {
        var origin = await attractions.GetDetailsByIdAsync(id, includeInactive: false, ct) ?? throw NotFound();
        var nearest = await attractions.GetNearestAsync(
            origin.Id, origin.Latitude, origin.Longitude, Math.Clamp(limit, 1, MaxNearby), ct);

        return nearest
            .Select(a => new NearbyAttractionDto(
                a.ToSummaryDto(),
                Math.Round(GeoDistance.StraightLineKm(origin.Latitude, origin.Longitude, a.Latitude, a.Longitude), 2)))
            .OrderBy(n => n.DistanceKm)
            .ToList();
    }

    public async Task<AttractionDetailsDto> GetForAdminAsync(int id, CancellationToken ct) =>
        (await attractions.GetDetailsByIdAsync(id, includeInactive: false, ct) ?? throw NotFound()).ToDetailsDto();

    public async Task<AttractionDetailsDto> CreateAsync(AttractionUpsertRequest request, CancellationToken ct)
    {
        var category = await ResolveCategoryAsync(request.Category, ct);
        var slug = request.Slug.Trim();
        if (await attractions.ActiveSlugExistsAsync(slug, exceptId: null, ct))
        {
            throw SlugTaken();
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var attraction = new Attraction { CreatedAtUtc = now };
        Apply(attraction, request, category, now);
        attractions.Add(attraction);
        await unitOfWork.SaveChangesAsync(ct);
        return attraction.ToDetailsDto();
    }

    public async Task<AttractionDetailsDto> UpdateAsync(int id, AttractionUpsertRequest request, CancellationToken ct)
    {
        var attraction = await attractions.GetForUpdateAsync(id, ct) ?? throw NotFound();
        var category = await ResolveCategoryAsync(request.Category, ct);
        if (await attractions.ActiveSlugExistsAsync(request.Slug.Trim(), exceptId: id, ct))
        {
            throw SlugTaken();
        }

        Apply(attraction, request, category, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return attraction.ToDetailsDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var attraction = await attractions.GetForUpdateAsync(id, ct) ?? throw NotFound();
        attraction.IsActive = false;
        attraction.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<AttractionCategory> ResolveCategoryAsync(string slug, CancellationToken ct) =>
        await categories.GetActiveBySlugAsync(slug.Trim().ToLowerInvariant(), ct)
        ?? throw new BusinessRuleException("invalid-category", "Choose a valid category.",
            new Dictionary<string, string[]> { ["category"] = ["Choose a valid category."] });

    private static void Apply(Attraction a, AttractionUpsertRequest r, AttractionCategory category, DateTime now)
    {
        var description = CleanList(r.Description);
        var errors = new Dictionary<string, string[]>();
        if (description.Count == 0)
        {
            errors["description"] = ["Add at least one description paragraph."];
        }
        else if (description.Any(p => p.Length > MaxParagraphLength))
        {
            errors["description"] = [$"Keep each paragraph under {MaxParagraphLength} characters."];
        }
        var tips = CleanList(r.VisitorTips);
        if (tips.Any(t => t.Length > MaxTipLength))
        {
            errors["visitorTips"] = [$"Keep each tip under {MaxTipLength} characters."];
        }
        var tags = CleanList(r.Tags).DistinctBy(t => t.ToLowerInvariant()).ToList();
        if (tags.Any(t => t.Length > MaxTagLength))
        {
            errors["tags"] = [$"Keep each tag under {MaxTagLength} characters."];
        }
        if (errors.Count > 0)
        {
            throw new BusinessRuleException("validation-failed", "Please fix the highlighted fields.", errors);
        }

        a.Name = r.Name.Trim();
        a.Slug = r.Slug.Trim();
        a.CategoryId = category.Id;
        a.Category = category;
        a.ShortDescription = r.ShortDescription.Trim();
        a.Description = description;
        a.ImageUrl = r.ImageUrl.Trim();
        a.ImageAlt = r.ImageAlt.Trim();
        a.ImageCreditLabel = r.ImageCredit?.Label.Trim();
        a.ImageCreditSourceUrl = r.ImageCredit?.SourceUrl.Trim();
        a.Address = r.Address.Trim();
        a.Area = r.Area.Trim();
        a.Latitude = r.Latitude;
        a.Longitude = r.Longitude;
        a.CoordinatesApproximate = r.CoordinatesApproximate;
        a.EstimatedVisitDurationMinutes = r.EstimatedVisitDurationMinutes;
        a.OpeningHours = NullIfBlank(r.OpeningHours);
        a.EntranceFee = NullIfBlank(r.EntranceFee);
        a.VisitorTips = tips;
        a.Tags = tags;
        a.IsFeatured = r.Featured;
        a.SearchText = SearchTextNormalizer.BuildAttractionSearchText(a.Name, a.Area, category.Name, tags);
        a.UpdatedAtUtc = now;

        a.Gallery.Clear();
        a.Gallery.AddRange(r.Gallery
            .DistinctBy(g => g.Url.Trim())
            .Select((g, i) => new AttractionImage
            {
                Url = g.Url.Trim(),
                AltText = g.Alt.Trim(),
                CreditLabel = g.Credit?.Label.Trim(),
                CreditSourceUrl = g.Credit?.SourceUrl.Trim(),
                SortOrder = i + 1,
            }));
    }

    private static List<string> CleanList(IEnumerable<string>? values) =>
        (values ?? []).Select(v => v?.Trim() ?? string.Empty).Where(v => v.Length > 0).ToList();

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DurationFilter ParseDuration(string? value) => value switch
    {
        null or "" or "any" => DurationFilter.Any,
        "short" => DurationFilter.Short,
        "medium" => DurationFilter.Medium,
        "long" => DurationFilter.Long,
        _ => throw new BusinessRuleException("invalid-duration", "Duration must be one of: any, short, medium, long."),
    };

    private static AttractionSort ParseSort(string? value) => value switch
    {
        null or "" or "recommended" => AttractionSort.Recommended,
        "name" => AttractionSort.Name,
        "duration-asc" => AttractionSort.DurationAscending,
        "duration-desc" => AttractionSort.DurationDescending,
        _ => throw new BusinessRuleException("invalid-sort", "Sort must be one of: recommended, name, duration-asc, duration-desc."),
    };

    private static NotFoundException NotFound() =>
        new("attraction-not-found", "We couldn't find that attraction. It may have been removed.");

    private static ConflictException SlugTaken() =>
        new("slug-taken", "Another attraction already uses this URL slug.",
            new Dictionary<string, string[]> { ["slug"] = ["Another attraction already uses this URL slug."] });
}
