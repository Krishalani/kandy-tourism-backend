using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Application.Services;
using KandyTourism.Api.Configuration;
using KandyTourism.Api.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace KandyTourism.Api.Infrastructure.Seed;

/// <summary>
/// Creates the first admin from <c>BootstrapAdmin</c> settings (user-secrets or environment variables).
/// No credentials are ever seeded from source code, and an existing account is never modified.
/// Further admins are created by a signed-in admin through <c>POST /api/v1/admin/users</c>.
/// </summary>
public sealed class AdminBootstrapper(
    IAdminUserService admins,
    IAdminUserRepository users,
    IOptions<BootstrapAdminOptions> options,
    ILogger<AdminBootstrapper> logger)
{
    public async Task EnsureAdminAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrEmpty(settings.Password))
        {
            return;
        }

        if (await users.GetByNormalizedEmailAsync(AdminAuthService.NormalizeEmail(settings.Email), ct) is not null)
        {
            logger.LogInformation("The bootstrap admin account already exists; BootstrapAdmin settings can be removed.");
            return;
        }

        try
        {
            var admin = await admins.CreateAsync(new CreateAdminRequest
            {
                Email = settings.Email,
                DisplayName = settings.DisplayName,
                Password = settings.Password,
            }, ct);
            logger.LogInformation("Created admin account {AdminId} from BootstrapAdmin settings.", admin.Id);
        }
        catch (BusinessRuleException error)
        {
            logger.LogError("BootstrapAdmin settings are invalid ({Reason}). The admin account was not created.", error.Message);
        }
    }
}
