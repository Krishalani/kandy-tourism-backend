using KandyTourism.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KandyTourism.Api.Infrastructure.Persistence.Configurations;

public sealed class AttractionCategoryConfiguration : IEntityTypeConfiguration<AttractionCategory>
{
    public void Configure(EntityTypeBuilder<AttractionCategory> builder)
    {
        builder.ToTable("AttractionCategories");
        builder.Property(c => c.Slug).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(80).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(300);
        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

public sealed class AttractionConfiguration : IEntityTypeConfiguration<Attraction>
{
    public void Configure(EntityTypeBuilder<Attraction> builder)
    {
        builder.ToTable("Attractions", t =>
        {
            t.HasCheckConstraint("CK_Attractions_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
            t.HasCheckConstraint("CK_Attractions_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
            t.HasCheckConstraint("CK_Attractions_VisitDuration",
                "[EstimatedVisitDurationMinutes] IS NULL OR ([EstimatedVisitDurationMinutes] >= 5 AND [EstimatedVisitDurationMinutes] <= 720)");
        });

        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(80).IsRequired();
        builder.Property(a => a.ShortDescription).HasMaxLength(220).IsRequired();
        builder.PrimitiveCollection(a => a.Description).IsRequired();
        builder.Property(a => a.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ImageAlt).HasMaxLength(200).IsRequired();
        builder.Property(a => a.ImageCreditLabel).HasMaxLength(100);
        builder.Property(a => a.ImageCreditSourceUrl).HasMaxLength(500);
        builder.Property(a => a.Address).HasMaxLength(160).IsRequired();
        builder.Property(a => a.Area).HasMaxLength(80).IsRequired();
        builder.Property(a => a.OpeningHours).HasMaxLength(200);
        builder.Property(a => a.EntranceFee).HasMaxLength(200);
        builder.PrimitiveCollection(a => a.VisitorTips).IsRequired();
        builder.PrimitiveCollection(a => a.Tags).IsRequired();
        builder.Property(a => a.SearchText).HasMaxLength(1000).IsRequired();

        // Only one *active* attraction may use a slug; deleted (inactive) rows keep theirs for history.
        builder.HasIndex(a => a.Slug).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(a => new { a.IsActive, a.IsFeatured });

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Attractions)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Gallery)
            .WithOne(i => i.Attraction)
            .HasForeignKey(i => i.AttractionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AttractionImageConfiguration : IEntityTypeConfiguration<AttractionImage>
{
    public void Configure(EntityTypeBuilder<AttractionImage> builder)
    {
        builder.ToTable("AttractionImages");
        builder.Property(i => i.Url).HasMaxLength(500).IsRequired();
        builder.Property(i => i.AltText).HasMaxLength(200).IsRequired();
        builder.Property(i => i.CreditLabel).HasMaxLength(100);
        builder.Property(i => i.CreditSourceUrl).HasMaxLength(500);
        builder.HasIndex(i => new { i.AttractionId, i.SortOrder });
    }
}

public sealed class StartPresetConfiguration : IEntityTypeConfiguration<StartPreset>
{
    public void Configure(EntityTypeBuilder<StartPreset> builder)
    {
        builder.ToTable("StartPresets", t =>
        {
            t.HasCheckConstraint("CK_StartPresets_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
            t.HasCheckConstraint("CK_StartPresets_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
        });
        builder.Property(p => p.Slug).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Label).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
