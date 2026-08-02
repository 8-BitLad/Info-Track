using InfoTrack.Scraper.Html;

namespace InfoTrack.Scraper;

public sealed class ScraperConfigOptions
{
    public string? TargetUrl { get; set; }
    public ScraperSelectorOptions Selectors { get; set; } = new();
}

public sealed class ScraperSelectorOptions
{
    public HtmlSelector Listing { get; set; } = new();
    public HtmlSelector Rating { get; set; } = new();
    public HtmlSelector Name { get; set; } = new();
    public HtmlSelector Phone { get; set; } = new();
    public HtmlSelector Email { get; set; } = new();
    public HtmlSelector? Website { get; set; }
    public HtmlSelector? Address { get; set; }
    public HtmlSelector? ReviewsCount { get; set; }
}