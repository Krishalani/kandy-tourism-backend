using KandyTourism.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KandyTourism.Api.Infrastructure.Persistence.Configurations;

public sealed class ContactSenderConfiguration : IEntityTypeConfiguration<ContactSender>
{
    public void Configure(EntityTypeBuilder<ContactSender> builder)
    {
        builder.ToTable("ContactSenders");
        builder.Property(s => s.Email).HasMaxLength(ContactSender.EmailMaxLength).IsRequired();
        builder.Property(s => s.NormalizedEmail).HasMaxLength(ContactSender.EmailMaxLength).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(ContactSender.NameMaxLength).IsRequired();
        builder.HasIndex(s => s.NormalizedEmail).IsUnique();
        builder.HasIndex(s => s.LastMessageAtUtc);

        builder.HasMany(s => s.Messages)
            .WithOne(m => m.Sender)
            .HasForeignKey(m => m.ContactSenderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.Property(m => m.Subject).HasMaxLength(ContactMessage.SubjectMaxLength).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(ContactMessage.BodyMaxLength).IsRequired();
        builder.HasIndex(m => new { m.ContactSenderId, m.ReceivedAtUtc });
        builder.HasIndex(m => m.ReadAtUtc);
    }
}
