using InfoTrack.Application.Commands;
using InfoTrack.Application.Queries;
using InfoTrack.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace InfoTrack.Application.Orchestrator
{
    public interface IScrapOrchestrator
    {
       Task GetSolicitorListingsAsync(
            string locationUrl,
            bool refresh = false,
            CancellationToken cancellationToken = default);

        Task GetSolicitorListingsByCityAsync(
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
        public async Task GetSolicitorListingsAsync(
            string locationUrl,
            bool refresh = false,
            CancellationToken cancellationToken = default)
        {
            
        }

        public async Task GetSolicitorListingsByCityAsync(
            string city,
            bool refresh = false,
            CancellationToken cancellationToken = default)
        {
           
        }
    }
}
