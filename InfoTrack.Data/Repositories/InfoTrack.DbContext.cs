using Microsoft.EntityFrameworkCore;
using InfoTrack.Domain;
using InfoTrack.Data.Entities;

namespace InfoTrack.Data;

public class InfoTrackDbContext(DbContextOptions<InfoTrackDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ScrapeRun> ScrapeRuns => Set<ScrapeRun>();
    public DbSet<SolicitorListing> SolicitorListings => Set<SolicitorListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Location configuration
        modelBuilder.Entity<Location>()
            .HasMany(l => l.ScrapeRuns)
            .WithOne(sr => sr.Location)
            .HasForeignKey(sr => sr.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ScrapeRun configuration
        modelBuilder.Entity<ScrapeRun>()
            .HasMany(sr => sr.Listings)
            .WithOne(sl => sl.ScrapeRun)
            .HasForeignKey(sl => sl.ScrapeRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
