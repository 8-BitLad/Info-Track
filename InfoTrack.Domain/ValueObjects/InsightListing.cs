namespace InfoTrack.Domain;

public sealed record InsightListing(
    string SolicitorName,
    string? PhoneNumber,
    string? Email,
    string? WebsiteUrl,
    int Rating,
    int ReviewsCount,
    string SourceUrl,
    string LocationName,
    string? LocationCounty,
    string LocationUrl,
    double ? Latitude,
    double ? Longitude,
    string? Address);
