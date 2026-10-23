using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KandyTourism.Api.Controllers;

/// <summary>The public contact form.</summary>
[ApiController]
[Route("api/v1/contact-messages")]
public sealed class ContactMessagesController(IContactService contacts) : ControllerBase
{
    /// <summary>
    /// Sends a message to the site admins. Messages from the same email address (ignoring case) are
    /// grouped under one contact.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(ServiceCollectionExtensions.ContactRateLimitPolicy)]
    [ProducesResponseType<ContactMessageReceiptDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ContactMessageReceiptDto>> Send(SendContactMessageRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await contacts.SendAsync(request, ct));
}

/// <summary>Contact-form messages, grouped by sender email. Requires an admin bearer token.</summary>
[ApiController]
[Route("api/v1/admin/contacts")]
[Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AdminContactsController(IContactService contacts) : ControllerBase
{
    /// <summary>Contacts (one per email address) with message counts, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ContactSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ContactSummaryDto>> List([FromQuery] ContactListQuery query, CancellationToken ct) =>
        contacts.SearchAsync(query, ct);

    /// <summary>A contact with all of their messages, newest first.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ContactDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ContactDetailsDto> Get(int id, CancellationToken ct) => contacts.GetAsync(id, ct);

    /// <summary>Marks every message from the contact as read or unread.</summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType<ContactDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ContactDetailsDto> SetAllRead(int id, UpdateReadStateRequest request, CancellationToken ct) =>
        contacts.SetAllReadAsync(id, request.IsRead!.Value, ct);

    /// <summary>Marks one message as read or unread.</summary>
    [HttpPut("{id:int}/messages/{messageId:int}/read")]
    [ProducesResponseType<ContactDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ContactDetailsDto> SetMessageRead(int id, int messageId, UpdateReadStateRequest request,
        CancellationToken ct) => contacts.SetMessageReadAsync(id, messageId, request.IsRead!.Value, ct);

    /// <summary>Permanently deletes the contact and all of their messages.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await contacts.DeleteAsync(id, ct);
        return NoContent();
    }
}
