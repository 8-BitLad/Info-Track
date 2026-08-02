using System.Text;

namespace InfoTrack.Scraper.Html;

public sealed class HtmlNode
{
    public string TagName { get; set; }
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<HtmlNode> Children { get; } = [];
    public HtmlNode? Parent { get; set; }
    public string? TextContent { get; set; }
    public bool IsTextNode => TagName == "#text";

    public string? GetAttribute(string name) =>
        Attributes.TryGetValue(name, out var value) ? value : null;

    public string InnerText
    {
        get
        {
            var builder = new StringBuilder();
            CollectText(this, builder);
            return builder.ToString();
        }
    }

    public string? FirstDirectText() =>
        Children.Where(c => c.IsTextNode && !string.IsNullOrWhiteSpace(c.TextContent))
                .Select(c => c.TextContent!.Trim())
                .FirstOrDefault();

    public string? LastDirectText() =>
        Children.Where(c => c.IsTextNode && !string.IsNullOrWhiteSpace(c.TextContent))
                .Select(c => c.TextContent!.Trim())
                .LastOrDefault();

    //return descendants of the node that are not text nodes
    public IEnumerable<HtmlNode> Descendants()
    {
        foreach (var child in Children)
        {
            if (!child.IsTextNode)
            {
                yield return child;
            }

            foreach (var descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    //iterates through the node and its children recursively to collect all text content
    private static void CollectText(HtmlNode node, StringBuilder builder)
    {
        if (node.IsTextNode)
        {
            builder.Append(node.TextContent);
            return;
        }

        foreach (var child in node.Children)
        {
            CollectText(child, builder);
        }
    }
}