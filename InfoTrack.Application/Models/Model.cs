using InfoTrack.Domain;

namespace InfoTrack.Application;

public sealed record SolicitorListingsResponse(string LocationUrl, string Source, IReadOnlyList<SolicitorCard> Listings);

public sealed class BootstrapOptions
{
    public string? CityURL { get; set; }
}