using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace KandyTourism.Api.Application.Services;

public sealed class AdminAuthService(
    IAdminUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher<AdminUser> passwordHasher,
    IAccessTokenService tokens,
    TimeProvider clock,
    ILogger<AdminAuthService> logger) : IAdminAuthService
{
    /// <summary>Verified against when the account doesn't exist, so response timing doesn't reveal valid emails.</summary>
    private static readonly Lazy<string> DummyHash =
        new(() => new PasswordHasher<AdminUser>().HashPassword(new AdminUser(), Guid.NewGuid().ToString()));

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByNormalizedEmailAsync(NormalizeEmail(request.Email), ct);
        if (user is null || !user.IsActive)
        {
            passwordHasher.VerifyHashedPassword(new AdminUser(), DummyHash.Value, request.Password);
            logger.LogWarning("Admin sign-in failed: unknown or inactive account.");
            throw InvalidCredentials();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Admin sign-in failed for admin {AdminId}: wrong password.", user.Id);
            throw InvalidCredentials();
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAtUtc = now;
        }
        user.LastLoginAtUtc = now;
        await unitOfWork.SaveChangesAsync(ct);

        var (token, expiresAtUtc) = tokens.CreateToken(user);
        logger.LogInformation("Admin {AdminId} signed in.", user.Id);
        return new LoginResponse(token, "Bearer", expiresAtUtc, ToProfile(user));
    }

    public async Task<AdminProfileDto> GetProfileAsync(int adminId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(adminId, ct);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("session-invalid", "Your admin session is no longer valid. Please sign in again.");
        }
        return ToProfile(user);
    }

    private static AdminProfileDto ToProfile(AdminUser user) => new(user.Id, user.Email, user.DisplayName, user.Role);

    private static UnauthorizedException InvalidCredentials() =>
        new("invalid-credentials", "The email or password is incorrect.");
}
