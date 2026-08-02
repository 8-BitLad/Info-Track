using InfoTrack.Application;
using InfoTrack.Application.Orchestrator;
using InfoTrack.Application.Queries;
using InfoTrack.Data;
using InfoTrack.Scraper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDataServices();
builder.Services.AddScrapingServices();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/api/locations/solicitors", async (
    string url,
    bool refresh,
    IScrapOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return Results.BadRequest(new { message = "A location URL is required." });
    }

    var listings = await orchestrator.GetSolicitorListingsAsync(url, refresh, cancellationToken);
    return Results.Ok(listings);
});

app.MapGet("/api/locations/city", async (
    string city,
    bool refresh,
    IScrapOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(city))
        return Results.BadRequest(new { message = "A city is required." });

    var listings = await orchestrator.GetSolicitorListingsByCityAsync(city, refresh, cancellationToken);
    return Results.Ok(listings);
});


app.MapDefaultEndpoints();

app.Run();