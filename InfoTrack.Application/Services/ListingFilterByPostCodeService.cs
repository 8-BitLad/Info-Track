using InfoTrack.Domain;
using InfoTrack.Domain.Contracts;
using System.Text.RegularExpressions;

namespace InfoTrack.Application.Services;

public interface IListingFilterByPostCodeService
{
    Task<IReadOnlyList<InsightListing>> SortByPostCodeAsync(
        IReadOnlyList<InsightListing> listings,
        string postCode,
        CancellationToken cancellationToken = default);
}

public sealed class ListingFilterByPostCodeService(
    ICoordinateLocator coordinateLocator) : IListingFilterByPostCodeService
{
    public async Task<IReadOnlyList<InsightListing>> SortByPostCodeAsync(
        IReadOnlyList<InsightListing> listings,
        string postCode,
        CancellationToken cancellationToken = default)
    {
        if (listings.Count == 0)
        {
            return listings;
        }

        // Geocode the user's postcode, if one was supplied
        (double? UserLatitude, double? UserLongitude) userCoordinates = (null, null);

        if (!string.IsNullOrWhiteSpace(postCode))
        {
            try
            {
                var coords = await coordinateLocator.GetCoordinatesAsync(postCode, cancellationToken);
                userCoordinates = (coords.Latitude, coords.Longitude);
            }
            catch (ArgumentException)
            {
                // If the user's postcode isn't found, just skip distance sorting
                userCoordinates = (null, null);
            }
        }

        // Enrich each listing with coordinates extracted from its address
        var enriched = new List<InsightListing>(listings.Count);

        foreach (var listing in listings)
        {
            var addressPostcode = ExtractPostcodeFromAddress(listing.Address);
            (double Latitude, double Longitude) listingCoordinates = (0.0, 0.0);

            if (!string.IsNullOrWhiteSpace(addressPostcode))
            {
                try
                {
                    listingCoordinates = await coordinateLocator.GetCoordinatesAsync(addressPostcode, cancellationToken);
                }
                catch (ArgumentException)
                {
                    // If the postcode isn't found for this listing, fall back to default coordinates
                    listingCoordinates = (0.0, 0.0);
                }
            }

            enriched.Add(listing with
            {
                Latitude = listingCoordinates.Latitude,
                Longitude = listingCoordinates.Longitude
            });
        }

        // Sort by distance from the user, if we have their coordinates
        if (userCoordinates.UserLatitude.HasValue && userCoordinates.UserLongitude.HasValue)
        {
            enriched = enriched
                .OrderBy(listing => CalculateDistance(
                    userCoordinates.UserLatitude.Value,
                    userCoordinates.UserLongitude.Value,
                    listing.Latitude ?? 0.0,
                    listing.Longitude ?? 0.0))
                .ToList();
        }

        return enriched.AsReadOnly();
    }

    private static string? ExtractPostcodeFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        // UK postcode pattern: e.g., "SW1A 1AA", "M1 1AE"
        var match = Regex.Match(address, @"([A-Z]{1,2}\d{1,2}[A-Z]?\s?\d[A-Z]{2})", RegexOptions.IgnoreCase);

        return match.Success ? match.Value.Trim() : null;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}