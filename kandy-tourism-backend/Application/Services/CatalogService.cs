using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;

namespace KandyTourism.Api.Application.Services;

public sealed class CatalogService(ICategoryRepository categories, IStartPresetRepository presets) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct) =>
        (await categories.GetActiveAsync(ct)).Select(c => new CategoryDto(c.Slug, c.Name, c.Description)).ToList();

    public async Task<IReadOnlyList<StartPointDto>> GetStartPointsAsync(CancellationToken ct) =>
        (await presets.GetActiveAsync(ct))
            .Select(p => new StartPointDto(p.Slug, p.Label, p.Description, p.Latitude, p.Longitude, p.CoordinatesApproximate, p.IsDefault))
            .ToList();
}
