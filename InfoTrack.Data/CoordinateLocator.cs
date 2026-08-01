using InfoTrack.Domain.Contracts;
using Postcod;

namespace InfoTrack.Application.Services;


public sealed class PostcodeCoordinateLocator : ICoordinateLocator
{
      public async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string postCode, CancellationToken cancellationToken = default)
    {
        var client = new PostcodeLookupClient();
        try
        {
            var location = await client.Search(postCode);

            if (location == null)
            {
                return await Task.FromResult((0.0, 0.0));
            }

            return  await Task.FromResult((location.Latitude ?? 0.0, location.Longitude ?? 0.0));
        }
        catch (HttpRequestException)
        {
            // The postcode lookup library can surface HTTP errors (e.g. 404).
            // Defensively return an empty location tuple instead of throwing.
            return await Task.FromResult((0.0, 0.0));
        }
        catch (Exception)
        {
            // Any other unexpected error from the library should also be treated defensively.
            return await Task.FromResult((0.0, 0.0));
        }
    }
}
