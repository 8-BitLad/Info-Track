namespace InfoTrack.Domain;

public sealed record SolicitorCard(
    string Name,
    string? PhoneNumber,
    string? Email,
    string? WebsiteUrl,
    int Rating,
    string SourceUrl,
    string? Address,
    int? ReviewsCount);