using InfoTrack.Application.Orchestrator;
using InfoTrack.Application.Queries;
using InfoTrack.Application.Services;
using InfoTrack.Domain.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Application;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplicationServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services
			.AddOptions<BootstrapOptions>()
			.Bind(configuration.GetSection("Bootstrap"));
        
        services.AddScoped<ICoordinateLocator, PostcodeCoordinateLocator>();       
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IListingFilterByPostCodeService, ListingFilterByPostCodeService>();
        services.AddScoped<ILocationQueryService, LocationQueryService>();
        services.AddScoped<IScrapOrchestrator, ScrapOrchestrator>();

        return services;
	}
}
