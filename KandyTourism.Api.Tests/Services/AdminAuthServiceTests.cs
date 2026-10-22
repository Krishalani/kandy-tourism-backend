using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Services;
using KandyTourism.Api.Configuration;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Exceptions;
using KandyTourism.Api.Infrastructure.Persistence;
using KandyTourism.Api.Infrastructure.Repositories;
using KandyTourism.Api.Infrastructure.Security;
using KandyTourism.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace KandyTourism.Api.Tests.Services;

public sealed class AdminAuthServiceTests : IDisposable
{
    private const string Password = "correct-horse-battery-staple";
    private readonly SqliteTestDatabase _database = new();
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public AdminAuthServiceTests()
    {
        using var db = _database.CreateContext();
        var admin = new AdminUser
        {
            Email = "Admin@Example.org",
            NormalizedEmail = AdminAuthService.NormalizeEmail("Admin@Example.org"),
            DisplayName = "Test Admin",
        };
        admin.PasswordHash = _hasher.HashPassword(admin, Password);
        db.AdminUsers.Add(admin);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private AdminAuthService CreateService(KandyTourismDbContext db) => new(
        new AdminUserRepository(db),
        new UnitOfWork(db),
        _hasher,
        new JwtAccessTokenService(Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = new string('k', 48),
            AccessTokenMinutes = 30,
        }), TimeProvider.System),
        TimeProvider.System,
        NullLogger<AdminAuthService>.Instance);

    [Fact]
    public async Task Valid_credentials_return_a_signed_admin_token()
    {
        await using var db = _database.CreateContext();

        var response = await CreateService(db).LoginAsync(
            new LoginRequest { Email = "  admin@example.ORG ", Password = Password }, CancellationToken.None);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("Admin", response.Admin.Role);
        Assert.InRange(response.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(response.AccessToken);
        Assert.Equal("Admin", jwt.GetClaim("role").Value);
        Assert.Equal(response.Admin.Id.ToString(), jwt.Subject);
        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains(Password));
    }

    [Theory]
    [InlineData("admin@example.org", "wrong-password")]
    [InlineData("nobody@example.org", Password)]
    public async Task Invalid_credentials_fail_with_the_same_generic_error(string email, string password)
    {
        await using var db = _database.CreateContext();

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService(db).LoginAsync(new LoginRequest { Email = email, Password = password }, CancellationToken.None));

        Assert.Equal("invalid-credentials", error.Code);
        Assert.Equal("The email or password is incorrect.", error.Message);
    }

    [Fact]
    public async Task Inactive_admins_cannot_sign_in()
    {
        await using var db = _database.CreateContext();
        db.AdminUsers.Single().IsActive = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService(db).LoginAsync(new LoginRequest { Email = "admin@example.org", Password = Password }, CancellationToken.None));
    }
}
