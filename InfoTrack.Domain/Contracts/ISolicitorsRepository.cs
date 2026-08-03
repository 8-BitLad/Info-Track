using InfoTrack.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace InfoTrack.Domain.Contract
{
    public interface ISolicitorsRepository
    {
        public Task<IReadOnlyList<DiscoveredLocation>> GetAllAsync(CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<SolicitorCard>> GetLatestListingsByLocationUrlAsync(string locationUrl, CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<InsightListing>> GetAllListingsWithLocationAsync(CancellationToken cancellationToken = default);
        public Task UpsertAsync(IEnumerable<DiscoveredLocation> locations, CancellationToken cancellationToken = default);
        public Task SaveListingsAsync(string locationUrl, IEnumerable<SolicitorCard> listings, CancellationToken cancellationToken = default);
    }
}
