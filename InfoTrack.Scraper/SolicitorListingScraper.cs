using InfoTrack.Domain;
using InfoTrack.Scraper.Html;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InfoTrack.Scraper;

public interface ISolicitorListingScraper
{
    Task<IReadOnlyList<SolicitorCard>> ScrapeAsync(string sourceUrl, CancellationToken cancellationToken = default);
}

public sealed class SolicitorListingScraper(
    HttpClient httpClient,
    IOptions<ScraperConfigOptions> options,
    ILogger<SolicitorListingScraper> logger) : ISolicitorListingScraper
{
    public async Task<IReadOnlyList<SolicitorCard>> ScrapeAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        string html = string.Empty;
        var selectors = options.Value.Selectors;

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("Invalid source URL: {SourceUrl}", sourceUrl);
            throw new ArgumentException("Source URL must be an HTTP or HTTPS URL.", nameof(sourceUrl));
        }

        try
        {
            html = await httpClient.GetStringAsync(sourceUri, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(ex, "Failed to fetch anything", sourceUri);
        }

        var cards = ExtractCards(html, selectors, sourceUri);

        if (cards.Count > 0)
        {
            logger.LogInformation("Scraped {CardCount} solicitor cards", cards.Count);
            return cards;
        }
 

        logger.LogWarning("No solicitor cards found from {SourceUrl}", sourceUrl);
        return [];
    }

    private static IReadOnlyList<SolicitorCard> ExtractCards(string html, ScraperSelectorOptions selectors, Uri sourceUri)
    {
        var document = HtmlParser.Parse(html);
        var listingNodes = document.FindAll(selectors.Listing);

        // someythig wrong with selectors
        if (listingNodes.Count == 0)
        {
            return [];
        }

        var cards = new List<SolicitorCard>(listingNodes.Count);

        
        foreach (var listingNode in listingNodes)
        {
            //< span class="h2">QualitySolicitors</span>
            var name = listingNode.FindFirst(selectors.Name)?.FirstDirectText();

            // no name, must be a non-solicitor listing, so skip it
            if (string.IsNullOrWhiteSpace(name))
                continue;

            //   <a rel = "noindex" href="tel:08082782864">0808 278 2864 </a>
            var phone = NormalizeLinkValue(listingNode.FindFirst(selectors.Phone)?.GetAttribute("href"), "tel:");

            //   <a href="mailto:">0808 278 2864 </a>
            var email = NormalizeLinkValue(listingNode.FindFirst(selectors.Email)?.GetAttribute("href"), "mailto:");

            // <a target = "_blank" href="https://www.qualitysolicitors.com" rel="nofollow">
            var websiteUrl = ReadWebsite(listingNode, selectors.Website, sourceUri);

            //<address>Qualitysolicitors</address>
            var address = selectors.Address is null ? null : listingNode.FindFirst(selectors.Address)?.InnerText.Trim();

            var reviewsText = selectors.ReviewsCount is null ? null : listingNode.FindFirst(selectors.ReviewsCount)?.LastDirectText();

            var reviewsCount = string.IsNullOrWhiteSpace(reviewsText) ? 0 : int.Parse(Regex.Replace(reviewsText, @"&nbsp;|[()\s]", string.Empty));

            //<span class="rev-results"><div class="star-full rating-lrg"></div>
            var rating = listingNode.FindAll(selectors.Rating).Count;

            cards.Add(new SolicitorCard(name, phone, email, websiteUrl, rating, sourceUri.AbsoluteUri, address, reviewsCount));
        }

        return cards.GroupBy(card => $"{card.Name}|{card.PhoneNumber}|{card.Email}", StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First())
                     .ToArray();
    }
    
    private static string? ReadWebsite(HtmlNode node, HtmlSelector? selector, Uri baseUri)
    {
        if (selector is null)
        {
            return null;
        }

        var href = node.FindFirst(selector)?.GetAttribute("href")?.Trim();

        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        return Uri.TryCreate(baseUri, href, out var absoluteUri) ? absoluteUri.AbsoluteUri : href;
    }

    private static string? NormalizeLinkValue(string? value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value).Trim();

        if (decoded.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            decoded = decoded[prefix.Length..];
        }

        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }
}