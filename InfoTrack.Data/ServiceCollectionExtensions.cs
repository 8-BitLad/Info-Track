using InfoTrack.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InfoTrack.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, string? databaseName = null)
    {
        databaseName ??= "InfoTrackDb";

        services.AddDbContext<InfoTrackDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
        );

        services.AddScoped<IDBRepository, DBRepository>();
        return services;
    }
}
