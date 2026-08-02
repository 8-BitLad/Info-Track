using System.Net;

namespace InfoTrack.Scraper.Html;

public static class HtmlParser
{   
    //build dom tree from html for navigation and extraction
    public static HtmlNode Parse(string html)
    {
        var root = new HtmlNode { TagName = "#root" };
        var stack = new Stack<HtmlNode>();
        stack.Push(root);
        int i = 0, len = html.Length;

        while (i < len)
        {
            if (html[i] != '<')
            {
                int next = html.IndexOf('<', i);
                int end = next == -1 ? len : next;
                AppendText(stack.Peek(), html[i..end]); //capture
                i = end;
                continue;
            }

            if (StartsWith(html, i, "<!--")) 
            {
                int end = html.IndexOf("-->", i, StringComparison.Ordinal);
                i = end == -1 ? len : end + 3;
                continue;
            }

            if (StartsWith(html, i, "<!"))
            {
                int end = html.IndexOf('>', i);
                i = end == -1 ? len : end + 1;
                continue;
            }

            bool isClosing = i + 1 < len && html[i + 1] == '/';
            int tagEnd = html.IndexOf('>', i);
            if (tagEnd == -1) break;

            if (isClosing)
            {
                var name = html[(i + 2)..tagEnd].Trim().ToLowerInvariant();
                while (stack.Count > 1 && !stack.Peek().TagName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    stack.Pop();
                if (stack.Count > 1) stack.Pop();
                i = tagEnd + 1;
                continue;
            }

            bool selfClosing = html[tagEnd - 1] == '/';
            var tagContent = html.Substring(i + 1, Math.Max(tagEnd - i - 1 - (selfClosing ? 1 : 0), 0));
            var (tagName, attrs) = ParseTag(tagContent);

            var node = new HtmlNode { TagName = tagName.ToLowerInvariant(), Parent = stack.Peek() };
            foreach (var (k, v) in attrs) node.Attributes[k] = v;
            stack.Peek().Children.Add(node);
            i = tagEnd + 1;

            if (RawTextElements.Contains(node.TagName))
            {
                int closeIdx = html.IndexOf($"</{node.TagName}", i, StringComparison.OrdinalIgnoreCase);
                int closeEnd = closeIdx == -1 ? -1 : html.IndexOf('>', closeIdx);
                i = closeEnd == -1 ? len : closeEnd + 1;
                continue;
            }

            if (!selfClosing && !VoidElements.Contains(node.TagName))
                stack.Push(node);
        }

        return root;
    }
    private static void AppendText(HtmlNode parent, string rawText)
    {
        var decoded = WebUtility.HtmlDecode(rawText);

        if (string.IsNullOrWhiteSpace(decoded))
        {
            return;
        }

        parent.Children.Add(new HtmlNode { TagName = "#text", TextContent = decoded, Parent = parent });
    }

    private static bool StartsWith(string html, int index, string token) =>
        index + token.Length <= html.Length &&
        string.Compare(html, index, token, 0, token.Length, StringComparison.OrdinalIgnoreCase) == 0;

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"
    };

    private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style"
    };

    private static (string TagName, List<(string Key, string Value)> Attributes) ParseTag(string tagContent)
    {
        tagContent = tagContent.Trim(); // remove whitespace
        var spaceIndex = tagContent.IndexOfAny([' ', '\t', '\n', '\r']);
        var tagName = spaceIndex == -1 ? tagContent : tagContent[..spaceIndex];
        var attributes = new List<(string, string)>();

        if (spaceIndex == -1)
        {
            return (tagName, attributes);
        }

        var attrText = tagContent[(spaceIndex + 1)..];
        var position = 0;

        while (position < attrText.Length)
        {
            while (position < attrText.Length && char.IsWhiteSpace(attrText[position])) position++;
            if (position >= attrText.Length) break;

            var nameStart = position;
            while (position < attrText.Length && attrText[position] != '=' && !char.IsWhiteSpace(attrText[position])) position++;
            var name = attrText[nameStart..position];

            if (string.IsNullOrWhiteSpace(name))
            {
                position++;
                continue;
            }

            while (position < attrText.Length && char.IsWhiteSpace(attrText[position])) position++;

            //crawl
            if (position < attrText.Length && attrText[position] == '=')
            {
                position++;
                while (position < attrText.Length && char.IsWhiteSpace(attrText[position])) position++;

                string value;
                if (position < attrText.Length && (attrText[position] == '"' || attrText[position] == '\''))
                {
                    var quote = attrText[position++];
                    var valueStart = position;
                    while (position < attrText.Length && attrText[position] != quote) position++;
                    value = attrText[valueStart..Math.Min(position, attrText.Length)];
                    position++;
                }
                else
                {
                    var valueStart = position;
                    while (position < attrText.Length && !char.IsWhiteSpace(attrText[position])) position++;
                    value = attrText[valueStart..position];
                }

                attributes.Add((name, WebUtility.HtmlDecode(value)));
            }
            else
            {
                attributes.Add((name, string.Empty));
                position++;
            }
        }

        return (tagName, attributes); 
    }
}