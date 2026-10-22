using KandyTourism.Api.Application.Common;
using KandyTourism.Api.Infrastructure.Seed;
using KandyTourism.Api.Tests.Infrastructure;

namespace KandyTourism.Api.Tests.Services;

public sealed class SeedDataTests
{
    [Fact]
    public void Committed_sql_script_matches_the_seed_data()
    {
        var path = Path.Combine(RepositoryPaths.ApiProjectDirectory, SeedSqlScriptGenerator.RelativePath);
        var committed = File.ReadAllText(path).ReplaceLineEndings("\n");

        Assert.True(committed == SeedSqlScriptGenerator.Generate(),
            "SeedKandyAttractions.sql is out of date. Run `dotnet run -- generate-seed-sql` in the API project folder.");
    }

    [Fact]
    public void Seed_data_has_the_extracted_prototype_records_with_valid_references()
    {
        Assert.Equal(6, SeedData.Categories.Count);
        Assert.Equal(2, SeedData.StartPresets.Count);
        Assert.Equal(15, SeedData.Attractions.Count);
        Assert.Single(SeedData.StartPresets, p => p.IsDefault);

        var categorySlugs = SeedData.Categories.Select(c => c.Slug).ToHashSet();
        Assert.All(SeedData.Attractions, a => Assert.Contains(a.Category, categorySlugs));
        Assert.Equal(SeedData.Attractions.Count, SeedData.Attractions.Select(a => a.Slug).Distinct().Count());
        Assert.Equal(4, SeedData.Attractions.Count(a => a.CoordinatesApproximate));
        Assert.All(SeedData.Attractions, a =>
        {
            Assert.InRange(a.Latitude, 5.8, 10);
            Assert.InRange(a.Longitude, 79.4, 82);
        });
    }

    [Theory]
    [InlineData("St. Paul’s Church", "st. pauls church")]
    [InlineData("Café ÉLAN", "cafe elan")]
    public void Normalizer_matches_the_frontend_rules(string input, string expected) =>
        Assert.Equal(expected, SearchTextNormalizer.Normalize(input));
}
