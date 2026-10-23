using KandyTourism.Api.Application.DTOs;
using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Exceptions;

namespace KandyTourism.Api.Application.Services;

public sealed class ContactService(
    IContactRepository contacts,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ContactService> logger) : IContactService
{
    public async Task<ContactMessageReceiptDto> SendAsync(SendContactMessageRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var subject = request.Subject.Trim();
        var body = request.Message.Trim();
        EnsureNotBlank(name, subject, body);

        var now = clock.GetUtcNow().UtcDateTime;
        var normalizedEmail = AdminAuthService.NormalizeEmail(email);
        var sender = await contacts.GetByNormalizedEmailAsync(normalizedEmail, ct);
        if (sender is null)
        {
            sender = new ContactSender { Email = email, NormalizedEmail = normalizedEmail, FirstContactedAtUtc = now };
            contacts.Add(sender);
        }
        sender.Name = name;
        sender.LastMessageAtUtc = now;

        var message = new ContactMessage { Subject = subject, Body = body, ReceivedAtUtc = now };
        sender.Messages.Add(message);
        await unitOfWork.SaveChangesAsync(ct);

        // Personal details stay out of the logs; the IDs are enough to find the message.
        logger.LogInformation("Contact message {MessageId} received for contact {ContactId}.", message.Id, sender.Id);
        return new ContactMessageReceiptDto(message.Id, AsUtc(message.ReceivedAtUtc));
    }

    public async Task<PagedResult<ContactSummaryDto>> SearchAsync(ContactListQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, ContactListQuery.MaxPageSize);
        var search = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim();

        var (items, totalCount) = await contacts.SearchAsync(search, query.Status == "unread", page, pageSize, ct);
        return new PagedResult<ContactSummaryDto>(
            items.Select(c => new ContactSummaryDto(
                c.Id,
                c.Name,
                c.Email,
                c.MessageCount,
                c.UnreadCount,
                c.LastSubject,
                AsUtc(c.FirstContactedAtUtc),
                AsUtc(c.LastMessageAtUtc))).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<ContactDetailsDto> GetAsync(int contactId, CancellationToken ct) =>
        ToDetailsDto(await contacts.GetWithMessagesAsync(contactId, track: false, ct) ?? throw ContactNotFound());

    public async Task<ContactDetailsDto> SetMessageReadAsync(int contactId, int messageId, bool isRead, CancellationToken ct)
    {
        var sender = await contacts.GetWithMessagesAsync(contactId, track: true, ct) ?? throw ContactNotFound();
        var message = sender.Messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new NotFoundException("contact-message-not-found", "This message no longer exists.");
        SetRead(message, isRead, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailsDto(sender);
    }

    public async Task<ContactDetailsDto> SetAllReadAsync(int contactId, bool isRead, CancellationToken ct)
    {
        var sender = await contacts.GetWithMessagesAsync(contactId, track: true, ct) ?? throw ContactNotFound();
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var message in sender.Messages)
        {
            SetRead(message, isRead, now);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return ToDetailsDto(sender);
    }

    public async Task DeleteAsync(int contactId, CancellationToken ct)
    {
        var sender = await contacts.GetWithMessagesAsync(contactId, track: true, ct) ?? throw ContactNotFound();
        contacts.Remove(sender);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Contact {ContactId} and their messages were deleted.", contactId);
    }

    /// <summary>Data annotations check lengths before trimming, so whitespace-only values are rejected here.</summary>
    private static void EnsureNotBlank(string name, string subject, string body)
    {
        var errors = new Dictionary<string, string[]>();
        if (name.Length < 2)
        {
            errors["name"] = ["Please enter your name."];
        }
        if (subject.Length < 3)
        {
            errors["subject"] = ["Please add a subject."];
        }
        if (body.Length < ContactMessage.BodyMinLength)
        {
            errors["message"] = ["Please write between 10 and 2000 characters."];
        }
        if (errors.Count > 0)
        {
            throw new BusinessRuleException("invalid-contact-message", "Please fix the highlighted fields.", errors);
        }
    }

    private static void SetRead(ContactMessage message, bool isRead, DateTime now) =>
        message.ReadAtUtc = isRead ? message.ReadAtUtc ?? now : null;

    private static ContactDetailsDto ToDetailsDto(ContactSender sender) => new(
        sender.Id,
        sender.Name,
        sender.Email,
        sender.Messages.Count(m => m.ReadAtUtc is null),
        AsUtc(sender.FirstContactedAtUtc),
        AsUtc(sender.LastMessageAtUtc),
        sender.Messages
            .OrderByDescending(m => m.ReceivedAtUtc).ThenByDescending(m => m.Id)
            .Select(m => new ContactMessageDto(
                m.Id,
                m.Subject,
                m.Body,
                AsUtc(m.ReceivedAtUtc),
                m.ReadAtUtc is not null,
                m.ReadAtUtc is { } readAt ? AsUtc(readAt) : null))
            .ToList());

    /// <summary>SQL Server returns stored UTC values without a kind; mark them so JSON carries the "Z".</summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static NotFoundException ContactNotFound() =>
        new("contact-not-found", "This contact no longer exists.");
}
