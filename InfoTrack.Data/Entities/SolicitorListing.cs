namespace InfoTrack.Data.Entities;

public class SolicitorListing
{
    public int Id { get; set; }
    public int ScrapeRunId { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public int Rating { get; set; }
    public string SourceUrl { get; set; } = null!;
    public string? Address { get; set; }
    public int ReviewsCount { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    public ScrapeRun ScrapeRun { get; set; } = null!;
}
