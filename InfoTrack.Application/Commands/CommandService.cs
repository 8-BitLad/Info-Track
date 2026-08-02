using InfoTrack.Data.Repositories;
using InfoTrack.Domain;
using InfoTrack.Scraper;

namespace InfoTrack.Application.Commands
{
    public interface ICommandService
    {
        Task ScrapeAndPersistListingsAsync(string locationUrl, CancellationToken cancellationToken = default);
    }

    public sealed class CommandService(IDBRepository repository, 
       ISolicitorListingScraper solicitorListingScraper) : ICommandService
    {
        public async Task ScrapeAndPersistListingsAsync(string locationUrl, CancellationToken cancellationToken = default)
        {
            try
            {
               
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
