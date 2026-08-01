namespace InfoTrack.Data.Entities;

public class ScrapeRun
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public int ListingsFound { get; set; }
    public int ListingsAdded { get; set; }
    public int ListingsRemoved { get; set; }
    public int ListingsChanged { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Running; // Running, Completed, Failed

    public Location Location { get; set; } = null!;
    public ICollection<SolicitorListing> Listings { get; set; } = new List<SolicitorListing>();
}
