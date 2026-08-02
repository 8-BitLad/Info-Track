using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Scraper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScrapingServices(this IServiceCollection services)
    {
        services.AddOptions<ScraperConfigOptions>().BindConfiguration("ScraperConfig");

        // Register HttpClient for scraper with timeout configuration
        services.AddHttpClient<ISolicitorListingScraper, SolicitorListingScraper>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36");

            });

        return services;
    }
}