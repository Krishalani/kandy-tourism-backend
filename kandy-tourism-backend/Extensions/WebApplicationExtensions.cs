using KandyTourism.Api.Configuration;
using KandyTourism.Api.Infrastructure.Persistence;
using KandyTourism.Api.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KandyTourism.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Optionally migrates and seeds (development), then creates the bootstrap admin if configured.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        if (options.ApplyMigrationsOnStartup)
        {
            await services.GetRequiredService<KandyTourismDbContext>().Database.MigrateAsync();
        }
        if (options.SeedOnStartup)
        {
            await services.GetRequiredService<DatabaseSeeder>().SeedAsync();
        }
        await services.GetRequiredService<AdminBootstrapper>().EnsureAdminAsync();
    }
}
