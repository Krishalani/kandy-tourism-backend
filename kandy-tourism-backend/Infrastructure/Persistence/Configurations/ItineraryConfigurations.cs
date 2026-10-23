using KandyTourism.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KandyTourism.Api.Infrastructure.Persistence.Configurations;

public sealed class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("Itineraries", t =>
        {
            t.HasCheckConstraint("CK_Itineraries_Start",
                "([StartType] = 'Preset' AND [StartPresetId] IS NOT NULL) OR " +
                "([StartType] = 'Attraction' AND [StartAttractionId] IS NOT NULL) OR " +
                "([StartType] = 'Custom' AND [StartQuery] IS NOT NULL)");
        });

        builder.Property(i => i.AccessTokenHash).HasMaxLength(32).IsFixedLength().IsRequired();
        builder.Property(i => i.StartType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.StartQuery).HasMaxLength(200);
        builder.HasIndex(i => i.PublicId).IsUnique();
        builder.HasIndex(i => i.ShareId).IsUnique().HasFilter("[ShareId] IS NOT NULL");

        builder.HasOne(i => i.StartPreset)
            .WithMany()
            .HasForeignKey(i => i.StartPresetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.StartAttraction)
            .WithMany()
            .HasForeignKey(i => i.StartAttractionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Stops)
            .WithOne(s => s.Itinerary)
            .HasForeignKey(s => s.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ItineraryStopConfiguration : IEntityTypeConfiguration<ItineraryStop>
{
    public void Configure(EntityTypeBuilder<ItineraryStop> builder)
    {
        builder.ToTable("ItineraryStops", t => t.HasCheckConstraint("CK_ItineraryStops_Sequence", "[Sequence] > 0"));

        builder.HasIndex(s => new { s.ItineraryId, s.AttractionId }).IsUnique();
        builder.HasIndex(s => new { s.ItineraryId, s.Sequence }).IsUnique();
        builder.HasIndex(s => s.AttractionId);

        // Attractions are deactivated rather than deleted; a hard delete must not erase plans.
        builder.HasOne(s => s.Attraction)
            .WithMany()
            .HasForeignKey(s => s.AttractionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
