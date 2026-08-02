using InfoTrack.Data.Entities;
using InfoTrack.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InfoTrack.Data.Repositories;

public interface IDBRepository
{
    public Task<IReadOnlyList<DiscoveredLocation>> GetAllAsync(CancellationToken cancellationToken = default);
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
