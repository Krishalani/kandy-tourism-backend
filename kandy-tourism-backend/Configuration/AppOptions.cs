using System.ComponentModel.DataAnnotations;

namespace KandyTourism.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Applies pending EF Core migrations at startup. Intended for local development only.</summary>
    public bool ApplyMigrationsOnStartup { get; init; }

    /// <summary>Inserts missing seed records at startup (never overwrites existing rows).</summary>
    public bool SeedOnStartup { get; init; }
}

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 key, at least 32 bytes. Set in appsettings.json; override with the Jwt__SigningKey environment variable in production.</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    [Range(5, 24 * 60)]
    public int AccessTokenMinutes { get; init; } = 60;
}

/// <summary>Optional: creates the first admin account at startup if no admin with this email exists.</summary>
public sealed class BootstrapAdminOptions
{
    public const string Section = "BootstrapAdmin";

    public string? Email { get; init; }
    public string? Password { get; init; }
    public string DisplayName { get; init; } = "Administrator";
}

public sealed class RateLimitOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Sign-in attempts allowed per client IP per minute.</summary>
    public int LoginPermitLimit { get; init; } = 5;
}
