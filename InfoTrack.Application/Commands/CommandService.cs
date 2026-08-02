using InfoTrack.Data.Repositories;
using InfoTrack.Domain;
using InfoTrack.Scraper;

public interface ICommandService
{
    Task<IReadOnlyList<SolicitorCard>> ScrapeAndPersistListingsAsync(string locationUrl, CancellationToken cancellationToken = default);
}

public sealed class CommandService(IDBRepository repository,
   ISolicitorListingScraper solicitorListingScraper) : ICommandService
{
    public async Task<IReadOnlyList<SolicitorCard>> ScrapeAndPersistListingsAsync(string locationUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var scrapedListings = await solicitorListingScraper.ScrapeAsync(locationUrl, cancellationToken);

            await repository.SaveListingsAsync(locationUrl, scrapedListings, cancellationToken);

            return scrapedListings;
        }
        catch (Exception)
        {
            throw;
        }
    }
}