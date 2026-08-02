namespace InfoTrack.Scraper.Html;

public sealed class HtmlSelector
{
    public string Tag { get; set; } = string.Empty;
    public string? ClassContains { get; set; }
    public string? ClassEquals { get; set; }
    public string? HrefStartsWith { get; set; }
}

public static class HtmlSelectorExtensions
{
    public static HtmlNode? FindFirst(this HtmlNode scope, HtmlSelector selector) =>
        scope.Descendants().FirstOrDefault(node => Matches(node, selector));

    public static IReadOnlyList<HtmlNode> FindAll(this HtmlNode scope, HtmlSelector selector) =>
        scope.Descendants().Where(node => Matches(node, selector)).ToList();

    // make sure tags are matching
    private static bool Matches(HtmlNode node, HtmlSelector selector)
    {
        if (!string.IsNullOrWhiteSpace(selector.Tag) &&
            !node.TagName.Equals(selector.Tag, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var classValue = node.GetAttribute("class") ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(selector.ClassContains) &&
            !classValue.Contains(selector.ClassContains, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selector.ClassEquals) &&
            !classValue.Equals(selector.ClassEquals, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selector.HrefStartsWith))
        {
            var href = node.GetAttribute("href") ?? string.Empty;
            if (!href.StartsWith(selector.HrefStartsWith, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}