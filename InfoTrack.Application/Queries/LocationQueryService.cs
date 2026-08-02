using InfoTrack.Application.Services;
using InfoTrack.Data.Repositories;
using InfoTrack.Domain;


namespace InfoTrack.Application.Queries
{
    public interface ILocationQueryService
    {
        Task<SolicitorListingsResponse> GetSolicitorListingsAsync(string locationUrl, bool refresh = false, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<InsightListing>> GetInsightListingsAsync(CancellationToken cancellationToken = default, string postCode = "");
    }

    public sealed class LocationQueryService(IDBRepository repository, IListingGeoEnrichmentService geoEnrichmentService) : ILocationQueryService
    {
        public async Task<SolicitorListingsResponse> GetSolicitorListingsAsync(string locationUrl, bool refresh = false, CancellationToken cancellationToken = default)
            => new SolicitorListingsResponse(locationUrl, "database", await repository.GetLatestListingsByLocationUrlAsync(locationUrl, cancellationToken));

        public async Task<IReadOnlyList<InsightListing>> GetInsightListingsAsync(CancellationToken cancellationToken = default, string postCode = "")
        {
            var insights = await repository.GetAllListingsWithLocationAsync(cancellationToken);
            if (string.IsNullOrEmpty(postCode))
            {
                return insights;
            }
            else
            {
                var listings = await geoEnrichmentService.EnrichAndSortAsync(insights, postCode, cancellationToken);
                return listings;
            }
        }
    }
}
