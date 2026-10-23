using KandyTourism.Api.Domain.Entities;
using KandyTourism.Api.Domain.Enums;
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
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ItineraryStatus.Planning).HasSentinel(ItineraryStatus.Planning).IsRequired();
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Feedback)
            .WithOne(f => f.Itinerary)
            .HasForeignKey<TripFeedback>(f => f.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);

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

public sealed class TripFeedbackConfiguration : IEntityTypeConfiguration<TripFeedback>
{
    public void Configure(EntityTypeBuilder<TripFeedback> builder)
    {
        builder.ToTable("TripFeedback", t => t.HasCheckConstraint("CK_TripFeedback_Rating",
            $"[Rating] BETWEEN {TripFeedback.MinRating} AND {TripFeedback.MaxRating}"));
        builder.Property(f => f.Comment).HasMaxLength(TripFeedback.CommentMaxLength);
        builder.HasIndex(f => f.ItineraryId).IsUnique();
        builder.HasIndex(f => f.SubmittedAtUtc);

        builder.HasMany(f => f.StopRatings)
            .WithOne(r => r.Feedback)
            .HasForeignKey(r => r.TripFeedbackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripStopRatingConfiguration : IEntityTypeConfiguration<TripStopRating>
{
    public void Configure(EntityTypeBuilder<TripStopRating> builder)
    {
        builder.ToTable("TripStopRatings", t => t.HasCheckConstraint("CK_TripStopRatings_Rating",
            $"[Rating] BETWEEN {TripFeedback.MinRating} AND {TripFeedback.MaxRating}"));
        builder.HasIndex(r => new { r.TripFeedbackId, r.AttractionId }).IsUnique();
        builder.HasIndex(r => r.AttractionId);

        // Attractions are deactivated rather than deleted, so ratings keep their attraction.
        builder.HasOne(r => r.Attraction)
            .WithMany()
            .HasForeignKey(r => r.AttractionId)
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
