using System.Security.Claims;
using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace KandyTourism.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAdminAuthService auth) : ControllerBase
{
    /// <summary>Signs an admin in and returns a short-lived bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceCollectionExtensions.LoginRateLimitPolicy)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    /// <summary>The signed-in admin's profile.</summary>
    [HttpGet("me")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType<AdminProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<AdminProfileDto> Me(CancellationToken ct) =>
        auth.GetProfileAsync(int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct);
}

/// <summary>Attraction management. Requires an admin bearer token.</summary>
[ApiController]
[Route("api/v1/admin/attractions")]
[Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminAttractionsController(IAttractionService attractions) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<AttractionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AttractionDetailsDto> Get(int id, CancellationToken ct) => attractions.GetForAdminAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<AttractionDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttractionDetailsDto>> Create(AttractionUpsertRequest request, CancellationToken ct)
    {
        var created = await attractions.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AttractionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AttractionDetailsDto> Update(int id, AttractionUpsertRequest request, CancellationToken ct) =>
        attractions.UpdateAsync(id, request, ct);

    /// <summary>
    /// Deletes an attraction from the catalogue. The row is deactivated rather than removed, so guest
    /// itineraries that referenced it stay consistent (the stop simply stops being shown).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await attractions.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
public sealed class AdminDashboardController(IAdminDashboardService dashboard) : ControllerBase
{
    /// <summary>Catalogue and guest-plan totals for the admin dashboard.</summary>
    [HttpGet]
    [ProducesResponseType<AdminDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<AdminDashboardDto> Get(CancellationToken ct) => dashboard.GetAsync(ct);
}
