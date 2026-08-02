using System.Net;

namespace InfoTrack.Scraper.Html;

public static class HtmlParser
{
    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
       "br", "img", "input", "link", "meta"
    };

    private static readonly HashSet<string> ScriptOrStyle = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style"
    };

    //build dom tree from html for navigation and extraction
    // Any <!-- comments --> and <!DOCTYPE> are skipped
    // Any <script> and <style> inner content doesn't get added.
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

            // NEW: skip entirely, don't add to tree at all
            if (VoidElements.Contains(tagName))
            {
                i = tagEnd + 1;
                continue;
            }

            var node = new HtmlNode { TagName = tagName.ToLowerInvariant(), Parent = stack.Peek() };
            foreach (var (k, v) in attrs) node.Attributes[k] = v;
            stack.Peek().Children.Add(node);
            i = tagEnd + 1;

            // Skip script/style entirely — don't add to tree, and skip past their body too
            if (ScriptOrStyle.Contains(tagName))
            {
                i = tagEnd + 1;
                var closeIdx = html.IndexOf($"</{tagName}", i, StringComparison.OrdinalIgnoreCase);
                var closeEnd = closeIdx == -1 ? -1 : html.IndexOf('>', closeIdx);
                i = closeEnd == -1 ? len : closeEnd + 1;
                continue;
            }

            if (!selfClosing)
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

    

    private static (string TagName, List<(string Key, string Value)> Attributes) ParseTag(string tagContent)
    {
        tagContent = tagContent.Trim();
        var spaceIndex = tagContent.IndexOfAny([' ', '\t', '\n', '\r']); //till space is found, this is tag
        var tagName = spaceIndex == -1 ? tagContent : tagContent[..spaceIndex];
        var attributes = new List<(string, string)>();
        if (spaceIndex == -1) return (tagName, attributes);

        var s = tagContent[(spaceIndex + 1)..]; // all possible attributes
        int p = 0;
        void SkipWs() { while (p < s.Length && char.IsWhiteSpace(s[p])) p++; }

        while (p < s.Length)
        {
            SkipWs();
            if (p >= s.Length) break;

            var nameStart = p;
            while (p < s.Length && s[p] != '=' && !char.IsWhiteSpace(s[p])) p++; // i.e href = "abc.com"
            var name = s[nameStart..p];

            if (string.IsNullOrWhiteSpace(name)) { p++; continue; }

            SkipWs();

            if (p < s.Length && s[p] == '=')
            {
                p++;
                SkipWs();
                string value;
                if (p < s.Length && (s[p] == '"' || s[p] == '\''))
                {
                    var quote = s[p++];
                    var start = p;
                    while (p < s.Length && s[p] != quote) p++;
                    value = s[start..Math.Min(p, s.Length)];
                    p++;
                }
                else
                {
                    var start = p;
                    while (p < s.Length && !char.IsWhiteSpace(s[p])) p++;
                    value = s[start..p];
                }
                attributes.Add((name, WebUtility.HtmlDecode(value)));
            }
            else
            {
                attributes.Add((name, string.Empty));
                p++;
            }
        }

        return (tagName, attributes);
    }
}