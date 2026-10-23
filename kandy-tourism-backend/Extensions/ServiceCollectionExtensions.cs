using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Application.Services;
using KandyTourism.Api.Configuration;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Infrastructure.Persistence;
using KandyTourism.Api.Infrastructure.Repositories;
using KandyTourism.Api.Infrastructure.Security;
using KandyTourism.Api.Infrastructure.Seed;
using KandyTourism.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace KandyTourism.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";
    public const string AdminPolicy = "AdminOnly";
    public const string LoginRateLimitPolicy = "login";
    public const string ContactRateLimitPolicy = "contact";
    public const string ConnectionStringName = "KandyTourism";

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAttractionService, AttractionService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IItineraryService, ItineraryService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IContactService, ContactService>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName} in appsettings or user-secrets.");

        services.AddDbContext<KandyTourismDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAttractionRepository, AttractionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IStartPresetRepository, StartPresetRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<AdminBootstrapper>();
        services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.Section));
        return services;
    }

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var settings = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = string.IsNullOrEmpty(settings.SigningKey)
                        ? null
                        : JwtAccessTokenService.CreateSigningKey(settings.SigningKey),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtAccessTokenService.NameClaim,
                    RoleClaimType = JwtAccessTokenService.RoleClaim,
                };
                bearer.Events = new JwtBearerEvents
                {
                    // Tokens are stateless; re-check the account so a deactivated admin loses access immediately.
                    OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        var users = context.HttpContext.RequestServices.GetRequiredService<IAdminUserRepository>();
                        var user = int.TryParse(subject, out var id)
                            ? await users.GetByIdAsync(id, context.HttpContext.RequestAborted)
                            : null;
                        if (user is not { IsActive: true })
                        {
                            context.Fail("The admin account is no longer active.");
                        }
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(AdminUser.AdminRole));

        var limits = configuration.GetSection(RateLimitOptions.Section).Get<RateLimitOptions>() ?? new RateLimitOptions();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = limits.LoginPermitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy(ContactRateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.ContactPermitLimit,
                    Window = TimeSpan.FromMinutes(limits.ContactWindowMinutes),
                    QueueLimit = 0,
                }));
            options.OnRejected = async (context, ct) =>
            {
                var policy = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = policy == ContactRateLimitPolicy
                        ? "You've sent several messages in a short time. Please wait a few minutes and try again."
                        : "Too many sign-in attempts. Please wait a minute and try again.",
                };
                problem.Extensions["code"] = "rate-limited";
                await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problem });
            };
        });

        return services;
    }

    public static IServiceCollection AddApiPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
                // Validation error keys use the JSON (camelCase) names the Angular forms bind to.
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
                if (context.ProblemDetails is ValidationProblemDetails validation)
                {
                    // Query-string errors are keyed by C# property name; align them with the camelCase JSON contract.
                    foreach (var key in validation.Errors.Keys.Where(k => k.Length > 0 && char.IsUpper(k[0])).ToList())
                    {
                        validation.Errors.Remove(key, out var messages);
                        validation.Errors[System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(key)] = messages!;
                    }
                    context.ProblemDetails.Extensions.TryAdd("code", "validation-failed");
                    context.ProblemDetails.Detail ??= "Please fix the highlighted fields.";
                }
            };
        });
        services.AddExceptionHandler<AppExceptionHandler>();

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization", "X-Itinerary-Token")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Kandy Day Trip Planner API",
                    Version = "v1",
                    Description =
                        "Attractions, guest itineraries and admin management for the Kandy Day Trip Planner. " +
                        "Guest itinerary endpoints need the X-Itinerary-Token header returned when the plan is created. " +
                        "Admin endpoints need a bearer token from POST /api/v1/auth/login.",
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Admin access token from POST /api/v1/auth/login.",
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
