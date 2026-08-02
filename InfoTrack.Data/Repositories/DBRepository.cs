using InfoTrack.Data.Entities;
using InfoTrack.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InfoTrack.Data.Repositories;

public interface IDBRepository
{
    public Task<IReadOnlyList<DiscoveredLocation>> GetAllAsync(CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<SolicitorCard>> GetLatestListingsByLocationUrlAsync(string locationUrl, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<InsightListing>> GetAllListingsWithLocationAsync(CancellationToken cancellationToken = default);
    public Task UpsertAsync(IEnumerable<DiscoveredLocation> locations, CancellationToken cancellationToken = default);
    public Task SaveListingsAsync(string locationUrl, IEnumerable<SolicitorCard> listings, CancellationToken cancellationToken = default);
}

public sealed class DBRepository : IDBRepository
{
    private readonly InfoTrackDbContext _dbContext;

    public DBRepository(InfoTrackDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<DiscoveredLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .OrderBy(location => location.County)
            .ThenBy(location => location.Name)
            .Select(location => new DiscoveredLocation(location.Name, location.Url, location.County))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SolicitorCard>> GetLatestListingsByLocationUrlAsync(string locationUrl, CancellationToken cancellationToken = default)
    {
        var latestRunId = await _dbContext.ScrapeRuns
            .Where(run => run.Location.Url == locationUrl)
            .OrderByDescending(run => run.StartTime)
            .Select(run => (int?)run.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRunId is null)
        {
            return [];
        }

        return await _dbContext.SolicitorListings
            .DistinctBy(l => l.Address)
            .Where(listing => listing.ScrapeRunId == latestRunId.Value)
            .OrderBy(listing => listing.Name)
            .Select(listing => new SolicitorCard(
                listing.Name,
                listing.PhoneNumber,
                listing.Email,
                listing.WebsiteUrl,
                listing.Rating,
                listing.SourceUrl,
                listing.Address,
                listing.ReviewsCount))            
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InsightListing>> GetAllListingsWithLocationAsync(CancellationToken cancellationToken = default)
    {
        var latestRunIds = await _dbContext.ScrapeRuns
            .GroupBy(run => run.LocationId)
            .Select(group => group.OrderByDescending(run => run.StartTime).Select(run => run.Id).First())
            .ToListAsync(cancellationToken);

        if (latestRunIds.Count == 0)
        {
            return new List<InsightListing>().AsReadOnly();
        }

        var listings = await _dbContext.SolicitorListings
            .Where(listing => latestRunIds.Contains(listing.ScrapeRunId))
            .Include(listing => listing.ScrapeRun)
                .ThenInclude(run => run.Location)
            .OrderBy(listing => listing.ScrapeRun.Location.County)
            .ThenBy(listing => listing.ScrapeRun.Location.Name)
            .ThenBy(listing => listing.Name)            
            .ToListAsync(cancellationToken);

        var result = new List<InsightListing>();

        foreach (var listing in listings)
        {
            // Guard against null ScrapeRun or Location
            if (listing.ScrapeRun == null || listing.ScrapeRun.Location == null)
            {
                continue;
            }

            result.Add(new InsightListing(
                listing.Name,
                listing.PhoneNumber,
                listing.Email,
                listing.WebsiteUrl,
                listing.Rating,
                listing.ReviewsCount,
                listing.SourceUrl,
                listing.ScrapeRun.Location.Name,
                listing.ScrapeRun.Location.County,
                listing.ScrapeRun.Location.Url,
                null,
                null,
                listing.Address));
        }

        return result.AsReadOnly();
    }

    public async Task UpsertAsync(IEnumerable<DiscoveredLocation> locations, CancellationToken cancellationToken = default)
    {
        var normalizedLocations = locations
            .GroupBy(location => location.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        if (normalizedLocations.Length == 0)
        {
            return;
        }

        var urls = normalizedLocations.Select(location => location.Url).ToArray();

        var existingLocations = await _dbContext.Locations
            .Where(location => urls.Contains(location.Url))
            .ToDictionaryAsync(location => location.Url, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var discoveredLocation in normalizedLocations)
        {
            if (existingLocations.TryGetValue(discoveredLocation.Url, out var existingLocation))
            {
                // Update existing location
                existingLocation.Name = discoveredLocation.Name;
                existingLocation.County = discoveredLocation.County;
                existingLocation.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            // Add new location
            _dbContext.Locations.Add(new Location
            {
                Name = discoveredLocation.Name,
                Url = discoveredLocation.Url,
                County = discoveredLocation.County,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveListingsAsync(string locationUrl, IEnumerable<SolicitorCard> listings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locationUrl))
        {
            throw new ArgumentException("Location URL cannot be null or whitespace.", nameof(locationUrl));
        }

        var location = await _dbContext.Locations
            .FirstOrDefaultAsync(l => l.Url == locationUrl, cancellationToken);

        if (location is null)
        {
            location = new Location
            {
                Name = ToLocationName(locationUrl),
                Url = locationUrl,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Locations.Add(location);
        }

        var listingArray = listings.ToArray();

        var scrapeRun = new ScrapeRun
        {
            Location = location,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            ListingsFound = listingArray.Length,
            ListingsAdded = listingArray.Length,
            ListingsRemoved = 0
        };

        // Add solicitor listings
        foreach (var listing in listingArray)
        {
            if (scrapeRun.Listings.Any(l => l.Name == listing.Name))
            {
                continue;
            }
            scrapeRun.Listings.Add(new SolicitorListing
            {
                Name = listing.Name,
                PhoneNumber = listing.PhoneNumber,
                Email = listing.Email,
                WebsiteUrl = listing.WebsiteUrl,
                Rating = listing.Rating,
                ReviewsCount = listing.ReviewsCount ?? 0,
                SourceUrl = listing.SourceUrl,
                Address = listing.Address,
                DiscoveredAt = DateTime.UtcNow
            });
        }

        _dbContext.ScrapeRuns.Add(scrapeRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ToLocationName(string locationUrl)
    {
        if (!Uri.TryCreate(locationUrl, UriKind.Absolute, out var uri))
        {
            return locationUrl;
        }

        var part = Path.GetFileNameWithoutExtension(uri.AbsolutePath)
            .Replace('-', ' ')
            .Replace('+', ' ')
            .Trim();

        return string.IsNullOrWhiteSpace(part) ? locationUrl : part;
    }
}
