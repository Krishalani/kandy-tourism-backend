using KandyTourism.Api.Infrastructure.Persistence;
using KandyTourism.Api.Infrastructure.Seed;
using KandyTourism.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Tests.Integration;

/// <summary>Runs the standalone SQL script against a database created only by the migrations.</summary>
public sealed class SqlSeedScriptTests
{
    [Fact]
    public async Task Script_matches_the_migrated_schema_and_is_safe_to_run_twice()
    {
        var connectionString = ApiFactory.LocalDbConnectionString($"KandyTourism_SqlScript_{Guid.NewGuid():N}");
        var options = new DbContextOptionsBuilder<KandyTourismDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new KandyTourismDbContext(options);
        await db.Database.MigrateAsync();

        try
        {
            var script = await File.ReadAllTextAsync(Path.Combine(RepositoryPaths.ApiProjectDirectory, SeedSqlScriptGenerator.RelativePath));
            await Execute(connectionString, script);

            // An admin edit and a deletion must survive a second run.
            await db.Attractions.Where(a => a.Slug == "kandy-lake").ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, "Kandy Lake (edited)"));
            await db.Attractions.Where(a => a.Slug == "hanthana-mountain-range").ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false));
            await Execute(connectionString, script);

            Assert.Equal(6, await db.Categories.CountAsync());
            Assert.Equal(2, await db.StartPresets.CountAsync());
            Assert.Equal(15, await db.Attractions.CountAsync());
            Assert.Equal(14, await db.Attractions.CountAsync(a => a.IsActive));
            Assert.Equal(10, await db.AttractionImages.CountAsync());
            Assert.Equal("Kandy Lake (edited)", await db.Attractions.Where(a => a.Slug == "kandy-lake").Select(a => a.Name).SingleAsync());

            // Rows written by SQL must read back through EF exactly like seeder rows (JSON columns, search text).
            var temple = await db.Attractions.Include(a => a.Category).Include(a => a.Gallery)
                .SingleAsync(a => a.Slug == "temple-of-the-sacred-tooth-relic");
            var expected = DatabaseSeeder.ToEntity(SeedData.Attractions[0], temple.Category, DateTime.UtcNow);
            Assert.Equal(expected.Description, temple.Description);
            Assert.Equal(expected.Tags, temple.Tags);
            Assert.Equal(expected.VisitorTips, temple.VisitorTips);
            Assert.Equal(expected.SearchText, temple.SearchText);
            Assert.Equal(expected.Gallery.Select(g => g.Url), temple.Gallery.OrderBy(g => g.SortOrder).Select(g => g.Url));
            Assert.Equal(1, temple.Id);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>Runs the script with sqlcmd's default session options (QUOTED_IDENTIFIER OFF).</summary>
    private static async Task Execute(string connectionString, string script)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var sqlcmdDefaults = new SqlCommand("SET QUOTED_IDENTIFIER OFF;", connection))
        {
            await sqlcmdDefaults.ExecuteNonQueryAsync();
        }
        await using var command = new SqlCommand(script, connection);
        await command.ExecuteNonQueryAsync();
    }
}
