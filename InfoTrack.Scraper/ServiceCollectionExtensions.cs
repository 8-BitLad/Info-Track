using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Scraper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScrapingServices(this IServiceCollection services)
    {
        services.AddHttpClient<ISolicitorListingScraper, SolicitorListingScraper>();

        return services;
    }
}