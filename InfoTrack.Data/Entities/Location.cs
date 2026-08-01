namespace InfoTrack.Data.Entities;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? County { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ScrapeRun> ScrapeRuns { get; set; } = new List<ScrapeRun>();
}
