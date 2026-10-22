using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KandyTourism.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real API against a throwaway SQL Server LocalDB database created by the EF Core
/// migrations and dropped afterwards. Requires LocalDB (installed with Visual Studio / SQL Express).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "integration-admin@example.test";
    public const string AdminPassword = "integration-test-password-123";

    public static string LocalDbConnectionString(string database) =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _databaseName = $"KandyTourism_Tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" keeps development user-secrets and appsettings.Development.json out of the tests.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:KandyTourism", LocalDbConnectionString(_databaseName));
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("BootstrapAdmin:Email", AdminEmail);
        builder.UseSetting("BootstrapAdmin:Password", AdminPassword);
        builder.UseSetting("RateLimiting:LoginPermitLimit", "100");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<KandyTourismDbContext>().Database.EnsureDeletedAsync();
        }
        await DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "API";
}
