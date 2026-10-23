namespace KandyTourism.Api.Domain.Entities;

/// <summary>A message sent through the public contact form.</summary>
public class ContactMessage
{
    public const int SubjectMaxLength = 120;
    public const int BodyMinLength = 10;
    public const int BodyMaxLength = 2000;

    public int Id { get; set; }
    public int ContactSenderId { get; set; }
    public ContactSender Sender { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }
    /// <summary>When an admin marked the message as read; null while it is unread.</summary>
    public DateTime? ReadAtUtc { get; set; }
}
