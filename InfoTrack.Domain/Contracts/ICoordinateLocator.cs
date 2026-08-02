namespace InfoTrack.Domain.Contracts
{
    public interface ICoordinateLocator
    {
        Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string postCode, CancellationToken cancellationToken = default);
    }
}
