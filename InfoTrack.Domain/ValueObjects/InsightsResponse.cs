namespace InfoTrack.Domain;

public sealed record InsightsResponse(IReadOnlyList<InsightListing> Listings, bool HasData, string Message);
