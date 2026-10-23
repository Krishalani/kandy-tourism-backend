using System.ComponentModel.DataAnnotations;
using KandyTourism.Api.Domain.Entities;

namespace KandyTourism.Api.Application.DTOs;

/// <summary>Body of <c>POST /api/v1/contact-messages</c> (the public contact form).</summary>
public sealed record SendContactMessageRequest
{
    [Required, StringLength(ContactSender.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(ContactSender.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(ContactMessage.SubjectMaxLength, MinimumLength = 3)]
    public string Subject { get; init; } = string.Empty;

    [Required, StringLength(ContactMessage.BodyMaxLength, MinimumLength = ContactMessage.BodyMinLength,
        ErrorMessage = "Please write between 10 and 2000 characters.")]
    public string Message { get; init; } = string.Empty;
}

public sealed record ContactMessageReceiptDto(int Id, DateTime ReceivedAtUtc);

/// <summary>Query string for <c>GET /api/v1/admin/contacts</c>.</summary>
public sealed record ContactListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Matched against the contact's name and email.</summary>
    [StringLength(100)]
    public string? Q { get; init; }

    /// <summary>all | unread (contacts with at least one unread message).</summary>
    [RegularExpression("^(all|unread)$", ErrorMessage = "Status must be one of: all, unread.")]
    public string? Status { get; init; }

    [Range(1, 10_000)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}

/// <summary>One row of the admin contacts table: a unique email address and its messages.</summary>
public sealed record ContactSummaryDto(
    int Id,
    string Name,
    string Email,
    int MessageCount,
    int UnreadCount,
    string LastSubject,
    DateTime FirstContactedAtUtc,
    DateTime LastMessageAtUtc);

public sealed record ContactMessageDto(
    int Id,
    string Subject,
    string Message,
    DateTime ReceivedAtUtc,
    bool IsRead,
    DateTime? ReadAtUtc);

/// <summary>A contact with all their messages, newest first.</summary>
public sealed record ContactDetailsDto(
    int Id,
    string Name,
    string Email,
    int UnreadCount,
    DateTime FirstContactedAtUtc,
    DateTime LastMessageAtUtc,
    IReadOnlyList<ContactMessageDto> Messages);

public sealed record UpdateReadStateRequest
{
    [Required]
    public bool? IsRead { get; init; }
}

public sealed record RecentContactMessageDto(
    int MessageId,
    int ContactId,
    string Name,
    string Email,
    string Subject,
    DateTime ReceivedAtUtc,
    bool IsRead);
