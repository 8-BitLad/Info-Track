using InfoTrack.Application.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace InfoTrack.Application.Orchestrator
{
    public interface IScrapOrchestrator
    {
        Task<SolicitorListingsResponse> GetSolicitorListingsAsync(
             string locationUrl,
             bool refresh = false,
             CancellationToken cancellationToken = default);

        Task<SolicitorListingsResponse> GetSolicitorListingsByCityAsync(
            string city,
            bool refresh = false,
            CancellationToken cancellationToken = default);
    }

    public sealed class ScrapOrchestrator(
    ILocationQueryService queryService,
    ICommandService commandService,
    IOptions<BootstrapOptions> bootstrapOptions,
    ILogger<ScrapOrchestrator> logger) : IScrapOrchestrator
    {
        public async Task<SolicitorListingsResponse> GetSolicitorListingsAsync(
            string locationUrl,
            bool refresh = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationUrl))
            {
                throw new ArgumentException("Location URL is required.", nameof(locationUrl));
            }

            if (!refresh)
            {
                var existing = await queryService.GetSolicitorListingsAsync(locationUrl, refresh, cancellationToken);

                if (existing is not null && existing.Listings.Count > 0)
                {
                    logger.LogInformation("Listings from database: {ListingCount}", existing.Listings.Count);
                    return existing;
                }
            }

            try
            {
                var scraped = await commandService.ScrapeAndPersistListingsAsync(locationUrl, cancellationToken);
                logger.LogInformation("Listings scraped: {ListingCount}", scraped.Count);
                return new SolicitorListingsResponse(locationUrl, "scraper", scraped);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to scrape listings from {LocationUrl}", locationUrl);
                throw;
            }
        }

        public async Task<SolicitorListingsResponse> GetSolicitorListingsByCityAsync(
            string city,
            bool refresh = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("City is required.", nameof(city));
            }

            var cityUrlTemplate = bootstrapOptions.Value.CityURL;
            if (string.IsNullOrWhiteSpace(cityUrlTemplate))
            {
                throw new InvalidOperationException("CityURL is not configured.");
            }

            var formattedCity = city.Trim().ToLowerInvariant();
            formattedCity = Regex.Replace(formattedCity, @"\s+", "-");
            formattedCity = Regex.Replace(formattedCity, @"[^a-z0-9\-]+", string.Empty);

            var locationUrl = cityUrlTemplate.Replace("{city}", formattedCity);
            return await GetSolicitorListingsAsync(locationUrl, refresh, cancellationToken);
        }
    }
}
