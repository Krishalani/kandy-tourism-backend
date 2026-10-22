using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Application.Mapping;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace KandyTourism.Api.Application.Services;

public sealed class AdminUserService(
    IAdminUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher<AdminUser> passwordHasher,
    TimeProvider clock) : IAdminUserService
{
    public async Task<AdminProfileDto> CreateAsync(CreateAdminRequest request, CancellationToken ct)
    {
        // Also enforced here because the bootstrap admin is created from configuration, not a validated request.
        if (request.Password.Length < CreateAdminRequest.MinimumPasswordLength)
        {
            throw new BusinessRuleException("weak-password", "Use a password of at least 12 characters.",
                new Dictionary<string, string[]> { ["password"] = ["Use a password of at least 12 characters."] });
        }

        var email = request.Email.Trim();
        var normalizedEmail = AdminAuthService.NormalizeEmail(email);
        if (await users.GetByNormalizedEmailAsync(normalizedEmail, ct) is not null)
        {
            throw new ConflictException("admin-email-taken", "An admin account with this email already exists.",
                new Dictionary<string, string[]> { ["email"] = ["An admin account with this email already exists."] });
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var admin = new AdminUser
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            Role = AdminUser.AdminRole,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);
        users.Add(admin);
        await unitOfWork.SaveChangesAsync(ct);
        return admin.ToProfileDto();
    }
}
