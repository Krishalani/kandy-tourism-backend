namespace KandyTourism.Api.Domain.Entities;

/// <summary>
/// A person who has used the contact form, identified by their email address (one row per
/// address, compared case-insensitively). Every message they send is attached to this row.
/// </summary>
public class ContactSender
{
    public const int NameMaxLength = 80;
    public const int EmailMaxLength = 256;

    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    /// <summary>The name given with the most recent message.</summary>
    public string Name { get; set; } = string.Empty;

    public List<ContactMessage> Messages { get; set; } = [];

    public DateTime FirstContactedAtUtc { get; set; }
    public DateTime LastMessageAtUtc { get; set; }
}
