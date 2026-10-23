using KandyTourism.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KandyTourism.Api.Infrastructure.Persistence;

public class KandyTourismDbContext(DbContextOptions<KandyTourismDbContext> options) : DbContext(options)
{
    public DbSet<AttractionCategory> Categories => Set<AttractionCategory>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<AttractionImage> AttractionImages => Set<AttractionImage>();
    public DbSet<StartPreset> StartPresets => Set<StartPreset>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryStop> ItineraryStops => Set<ItineraryStop>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ContactSender> ContactSenders => Set<ContactSender>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KandyTourismDbContext).Assembly);
}
