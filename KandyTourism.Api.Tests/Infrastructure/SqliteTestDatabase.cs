using KandyTourism.Api.Application.Services;
using KandyTourism.Api.Infrastructure.Persistence;
using KandyTourism.Api.Infrastructure.Repositories;
using KandyTourism.Api.Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KandyTourism.Api.Tests.Infrastructure;

/// <summary>
/// A seeded, relational in-memory database for service tests. SQLite enforces the same unique
/// indexes, foreign keys and transactions as SQL Server for the behaviour under test.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<KandyTourismDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<KandyTourismDbContext>().UseSqlite(_connection).Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
        new DatabaseSeeder(db, TimeProvider.System, NullLogger<DatabaseSeeder>.Instance).SeedAsync().GetAwaiter().GetResult();
    }

    public KandyTourismDbContext CreateContext() => new(_options);

    public AttractionService CreateAttractionService(KandyTourismDbContext db) =>
        new(new AttractionRepository(db), new CategoryRepository(db), new UnitOfWork(db), TimeProvider.System);

    public ItineraryService CreateItineraryService(KandyTourismDbContext db) =>
        new(new ItineraryRepository(db), new AttractionRepository(db), new StartPresetRepository(db), new UnitOfWork(db), TimeProvider.System);

    public int AttractionId(string slug)
    {
        using var db = CreateContext();
        return db.Attractions.Single(a => a.Slug == slug).Id;
    }

    public void Dispose() => _connection.Dispose();
}
