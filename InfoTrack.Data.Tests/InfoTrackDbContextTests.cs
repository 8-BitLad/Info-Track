using InfoTrack.Data.Entities;
using InfoTrack.Domain;
using Microsoft.EntityFrameworkCore;

namespace InfoTrack.Data.Tests;

public class InfoTrackDbContextTests
{
    [Fact]
    public async Task SaveChanges_PersistsAScrapeRunWithItsListingSnapshot()
    {
        var options = CreateOptions();

        await using (var context = new InfoTrackDbContext(options))
        {
            var location = new Location
            {
                Name = "London",
                Url = "https://www.solicitors.com/conveyancing.html"
            };

            location.ScrapeRuns.Add(new ScrapeRun
            {
                Status = StatusEnum.Completed,
                ListingsFound = 1,
                EndTime = DateTime.UtcNow,
                Listings =
                {
                    new SolicitorListing
                    {
                        Name = "Example Conveyancing Solicitors",
                        PhoneNumber = "020 0000 0000",
                        Email = "hello@example.com",
                        WebsiteUrl = "https://www.solicitors.com/example-conveyancing.html",
                        Rating = 4,
                        SourceUrl = location.Url
                    }
                }
            });

            context.Locations.Add(location);
            await context.SaveChangesAsync();
        }

        await using var verificationContext = new InfoTrackDbContext(options);
        var storedRun = await verificationContext.ScrapeRuns
            .Include(run => run.Location)
            .Include(run => run.Listings)
            .SingleAsync();

        Assert.Equal("London", storedRun.Location.Name);
        Assert.Equal(StatusEnum.Completed, storedRun.Status);
        var listing = Assert.Single(storedRun.Listings);
        Assert.Equal("Example Conveyancing Solicitors", listing.Name);
    }

    [Fact]
    public async Task RemovingAScrapeRun_RemovesItsListings()
    {
        var options = CreateOptions();

        await using (var setupContext = new InfoTrackDbContext(options))
        {
            var location = new Location
            {
                Name = "Leeds",
                Url = "https://www.solicitors.com/conveyancing.html"
            };

            location.ScrapeRuns.Add(new ScrapeRun
            {
                Listings =
                {
                    new SolicitorListing
                    {
                        Name = "Example Leeds Solicitors",
                        PhoneNumber = "0113 000 0000",
                        Email = "contact@example.com",
                        WebsiteUrl = "https://www.solicitors.com/example-leeds.html",
                        Rating = 3,
                        SourceUrl = location.Url
                    }
                }
            });

            setupContext.Locations.Add(location);
            await setupContext.SaveChangesAsync();
        }

        await using (var deletionContext = new InfoTrackDbContext(options))
        {
            var run = await deletionContext.ScrapeRuns
                .Include(scrapeRun => scrapeRun.Listings)
                .SingleAsync();

            deletionContext.ScrapeRuns.Remove(run);
            await deletionContext.SaveChangesAsync();
        }

        await using var verificationContext = new InfoTrackDbContext(options);
        Assert.Empty(await verificationContext.ScrapeRuns.ToListAsync());
        Assert.Empty(await verificationContext.SolicitorListings.ToListAsync());
    }

    private static DbContextOptions<InfoTrackDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<InfoTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }
}
