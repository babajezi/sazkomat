using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;

namespace Sazkomat.Api.Endpoints;

/// <summary>
/// Endpoints for receiving data from external Tipsport scraper.
/// The external scraper runs outside Docker to bypass Cloudflare protection.
/// </summary>
public static class TipsportEndpoints
{
    public static void MapTipsportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tipsport")
            .WithTags("Tipsport External Scraper");

        group.MapPost("/leagues", ReceiveTipsportLeagues)
            .WithName("ReceiveTipsportLeagues")
            .WithDescription("Receive leagues from external Tipsport scraper");
    }

    private static async Task<IResult> ReceiveTipsportLeagues(
        [FromBody] TipsportLeaguesRequest request,
        DataDbContext dbContext,
        ILogger<Program> logger)
    {
        try
        {
            if (request.Leagues == null || request.Leagues.Count == 0)
            {
                return Results.BadRequest(new { error = "No leagues provided" });
            }

            logger.LogInformation("Receiving {Count} leagues from external Tipsport scraper", request.Leagues.Count);

            var providerId = Guid.Parse(request.ProviderId);
            var now = DateTime.UtcNow;
            var savedCount = 0;
            var updatedCount = 0;

            foreach (var league in request.Leagues)
            {
                // Check if already exists (using ProviderSlug as unique identifier)
                var existing = await dbContext.ProviderLeagues
                    .FirstOrDefaultAsync(pl =>
                        pl.ProviderId == providerId &&
                        pl.ProviderSlug == league.ProviderLeagueId);

                if (existing != null)
                {
                    // Update existing
                    existing.ProviderName = league.ProviderLeagueName;
                    existing.CountryCode = league.CountryCode;
                    existing.RawData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        url = league.Url,
                        matchCount = league.MatchCount,
                        scrapedAt = now
                    });
                    existing.UpdatedAt = now;
                    existing.ScrapedAt = now;
                    updatedCount++;
                }
                else
                {
                    // Create new
                    var providerLeague = new ProviderLeague
                    {
                        Id = Guid.NewGuid(),
                        ProviderId = providerId,
                        ProviderSlug = league.ProviderLeagueId,  // Store the ID as slug
                        ProviderName = league.ProviderLeagueName,
                        DisplayName = league.ProviderLeagueName,
                        CountryCode = league.CountryCode,
                        IsBettable = true,
                        ScrapedAt = now,
                        RawData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            url = league.Url,
                            matchCount = league.MatchCount,
                            scrapedAt = now
                        }),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    dbContext.ProviderLeagues.Add(providerLeague);
                    savedCount++;
                }
            }

            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Tipsport leagues saved: {New} new, {Updated} updated",
                savedCount, updatedCount);

            return Results.Ok(new
            {
                message = "Tipsport leagues received successfully",
                newLeagues = savedCount,
                updatedLeagues = updatedCount,
                totalReceived = request.Leagues.Count
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error receiving Tipsport leagues");
            return Results.Problem($"Error: {ex.Message}");
        }
    }
}

public class TipsportLeaguesRequest
{
    public string ProviderId { get; set; } = "";
    public List<TipsportLeagueItem> Leagues { get; set; } = new();
}

public class TipsportLeagueItem
{
    public string ProviderLeagueId { get; set; } = "";
    public string ProviderLeagueName { get; set; } = "";
    public string? CountryCode { get; set; }
    public string? Url { get; set; }
    public int MatchCount { get; set; }
}
