using KandyTourism.Api.Application.Services;
using KandyTourism.Api.Configuration;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KandyTourism.Api.Infrastructure.Seed;

/// <summary>
/// Creates the first admin from <c>BootstrapAdmin</c> settings (user-secrets or environment variables).
/// No credentials are ever seeded from source code, and an existing account is never modified.
/// </summary>
public sealed class AdminBootstrapper(
    KandyTourismDbContext db,
    IPasswordHasher<AdminUser> passwordHasher,
    IOptions<BootstrapAdminOptions> options,
    TimeProvider clock,
    ILogger<AdminBootstrapper> logger)
{
    public const int MinimumPasswordLength = 12;

    public async Task EnsureAdminAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrEmpty(settings.Password))
        {
            return;
        }
        if (settings.Password.Length < MinimumPasswordLength)
        {
            logger.LogError("BootstrapAdmin:Password must be at least {Length} characters. The admin account was not created.",
                MinimumPasswordLength);
            return;
        }

        var normalizedEmail = AdminAuthService.NormalizeEmail(settings.Email);
        if (await db.AdminUsers.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            logger.LogInformation("The bootstrap admin account already exists; BootstrapAdmin settings can be removed.");
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var admin = new AdminUser
        {
            Email = settings.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = settings.DisplayName.Trim(),
            Role = AdminUser.AdminRole,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, settings.Password);
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created admin account {AdminId} from BootstrapAdmin settings.", admin.Id);
    }
}
