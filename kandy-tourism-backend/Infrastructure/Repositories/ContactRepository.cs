using KandyTourism.Api.Application.Interfaces;
using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Infrastructure.Repositories;

public sealed class ContactRepository(KandyTourismDbContext db) : IContactRepository
{
    public Task<ContactSender?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.ContactSenders.FirstOrDefaultAsync(s => s.NormalizedEmail == normalizedEmail, ct);

    public async Task<(IReadOnlyList<ContactListItem> Items, int TotalCount)> SearchAsync(
        string? search, bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = db.ContactSenders.AsNoTracking();
        if (search is not null)
        {
            var normalized = search.ToUpperInvariant();
            query = query.Where(s => s.Name.Contains(search) || s.NormalizedEmail.Contains(normalized));
        }
        if (unreadOnly)
        {
            query = query.Where(s => s.Messages.Any(m => m.ReadAtUtc == null));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.LastMessageAtUtc).ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ContactListItem(
                s.Id,
                s.Name,
                s.Email,
                s.Messages.Count,
                s.Messages.Count(m => m.ReadAtUtc == null),
                s.Messages
                    .OrderByDescending(m => m.ReceivedAtUtc).ThenByDescending(m => m.Id)
                    .Select(m => m.Subject)
                    .FirstOrDefault() ?? string.Empty,
                s.FirstContactedAtUtc,
                s.LastMessageAtUtc))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<ContactSender?> GetWithMessagesAsync(int id, bool track, CancellationToken ct)
    {
        var senders = track ? db.ContactSenders : db.ContactSenders.AsNoTracking();
        return senders
            .Include(s => s.Messages.OrderByDescending(m => m.ReceivedAtUtc).ThenByDescending(m => m.Id))
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<ContactStatistics> GetStatisticsAsync(int recentCount, CancellationToken ct)
    {
        var contacts = await db.ContactSenders.CountAsync(ct);
        var messages = await db.ContactMessages.CountAsync(ct);
        var unread = await db.ContactMessages.CountAsync(m => m.ReadAtUtc == null, ct);
        var recent = await db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.ReceivedAtUtc).ThenByDescending(m => m.Id)
            .Take(recentCount)
            .Select(m => new RecentContactMessage(
                m.Id,
                m.ContactSenderId,
                m.Sender.Name,
                m.Sender.Email,
                m.Subject,
                m.ReceivedAtUtc,
                m.ReadAtUtc != null))
            .ToListAsync(ct);

        return new ContactStatistics(contacts, messages, unread, recent);
    }

    public void Add(ContactSender sender) => db.ContactSenders.Add(sender);

    public void Remove(ContactSender sender) => db.ContactSenders.Remove(sender);
}
