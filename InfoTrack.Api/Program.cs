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

app.MapGet("/api/insights/listings", async (
    ILocationQueryService service,
    string? postCode,
    CancellationToken cancellationToken) =>
{
    var response = await service.GetInsightListingsAsync(cancellationToken, postCode: postCode ?? "");
    return Results.Ok(response);
});


app.MapDefaultEndpoints();

app.Run();