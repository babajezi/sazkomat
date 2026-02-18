using System.Text.Json;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.Api.Endpoints;

public static class BetExplorerEndpoints
{
    private static readonly Guid BetExplorerProviderId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    public static void MapBetExplorerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/betexplorer")
            .WithTags("BetExplorer")
            .WithOpenApi();

        // GET /api/betexplorer/leagues/{countryCode}
        // Returns leagues from BetExplorer for a given country (with caching)
        group.MapGet("/leagues/{countryCode}", async (
            string countryCode,
            bool forceRefresh,
            ICountryRepository countryRepo,
            IProviderLeagueRepository providerLeagueRepo,
            ISportRepository sportRepo,
            IDataProviderRepository providerRepo,
            IEnumerable<ILeagueMetadataScraper> scrapers,
            ILogger<Program> logger) =>
        {
            // Find country by code
            var country = await countryRepo.GetByCodeAsync(countryCode);
            if (country == null)
            {
                // Try to find by name as fallback
                var allCountries = await countryRepo.GetAllAsync();
                country = allCountries.FirstOrDefault(c =>
                    c.Name.Equals(countryCode, StringComparison.OrdinalIgnoreCase) ||
                    c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase));

                if (country == null)
                {
                    return Results.NotFound(new { error = $"Country '{countryCode}' not found" });
                }
            }

            // Check cache first (unless forceRefresh)
            if (!forceRefresh)
            {
                // Get all leagues for this provider and filter by country code
                var allProviderLeagues = await providerLeagueRepo.GetByProviderIdAsync(BetExplorerProviderId);
                var cachedLeagues = allProviderLeagues
                    .Where(l => l.CountryCode?.Equals(country.Code, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                // Use cache if it exists and is less than 24 hours old
                var recentCache = cachedLeagues
                    .Where(l => l.ScrapedAt > DateTime.UtcNow.AddHours(-24))
                    .ToList();

                if (recentCache.Count > 0)
                {
                    logger.LogInformation("Returning {Count} cached BetExplorer leagues for {Country}",
                        recentCache.Count, country.Name);

                    return Results.Ok(recentCache.Select(l => new BetExplorerLeagueDto
                    {
                        Name = l.ProviderName,
                        Slug = l.ProviderSlug,
                        DisplayName = $"{l.ProviderName} ({country.Name})",
                        FromCache = true,
                        CachedAt = l.ScrapedAt
                    }).OrderBy(l => l.Name));
                }
            }

            // Get football sport
            var footballSport = (await sportRepo.GetAllAsync())
                .FirstOrDefault(s => s.Code.Equals("football", StringComparison.OrdinalIgnoreCase));

            if (footballSport == null)
            {
                return Results.BadRequest(new { error = "Football sport not found in database" });
            }

            // Find BetExplorer scraper
            var scraper = scrapers.FirstOrDefault(s =>
                s is BetExplorerLeagueMetadataScraper);

            if (scraper == null)
            {
                return Results.BadRequest(new { error = "BetExplorer scraper not available" });
            }

            logger.LogInformation("Scraping BetExplorer leagues for {Country}", country.Name);

            // Get BetExplorer provider to read CurrentSeasonPatterns
            var provider = await providerRepo.GetByIdAsync(BetExplorerProviderId);
            if (provider == null)
            {
                return Results.BadRequest(new { error = "BetExplorer provider not found in database" });
            }

            // Scrape leagues from current season only
            var allLeagues = new List<LeagueMetadata>();

            // Get season patterns from provider configuration
            List<string> seasonPatterns;
            try
            {
                seasonPatterns = JsonSerializer.Deserialize<List<string>>(provider.CurrentSeasonPatterns) ?? new List<string>();
                if (seasonPatterns.Count == 0)
                {
                    logger.LogWarning("No current season patterns configured for BetExplorer provider");
                    return Results.BadRequest(new { error = "No current season patterns configured for BetExplorer" });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize current season patterns for BetExplorer");
                return Results.BadRequest(new { error = "Invalid season patterns configuration" });
            }

            logger.LogInformation("Using provider's current season patterns: {Seasons}", string.Join(", ", seasonPatterns));

            // Try season-specific scraping first
            foreach (var pattern in seasonPatterns)
            {
                try
                {
                    var seasonLeagues = await ((BetExplorerLeagueMetadataScraper)scraper)
                        .ScrapeLeaguesForCurrentSeasonAsync(footballSport, country, new List<string> { pattern });

                    foreach (var league in seasonLeagues)
                    {
                        if (!allLeagues.Any(l => l.Slug.Equals(league.Slug, StringComparison.OrdinalIgnoreCase)))
                        {
                            allLeagues.Add(league);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to scrape season {Season} for {Country}", pattern, country.Name);
                }
            }

            // If no leagues found from seasons, try generic scrape
            if (allLeagues.Count == 0)
            {
                logger.LogInformation("No season-specific leagues found, trying generic scrape");
                var genericLeagues = await scraper.ScrapeLeaguesAsync(footballSport, country);
                allLeagues.AddRange(genericLeagues);
            }

            logger.LogInformation("Found {Count} unique leagues for {Country}", allLeagues.Count, country.Name);

            // Save to cache (provider_leagues)
            foreach (var league in allLeagues)
            {
                // Check if already exists
                var existing = await providerLeagueRepo.GetByProviderSlugAsync(
                    BetExplorerProviderId, league.Slug);

                if (existing == null)
                {
                    var providerLeague = new Data.Entities.ProviderLeague
                    {
                        ProviderId = BetExplorerProviderId,
                        ProviderSlug = league.Slug,
                        ProviderName = league.Name,
                        DisplayName = league.DisplayName,
                        CountryCode = country.Code,
                        IsImported = false,
                        ScrapedAt = DateTime.UtcNow
                    };
                    await providerLeagueRepo.CreateAsync(providerLeague);
                }
                else
                {
                    // Update cache timestamp
                    existing.ScrapedAt = DateTime.UtcNow;
                    await providerLeagueRepo.UpdateAsync(existing);
                }
            }

            return Results.Ok(allLeagues.Select(l => new BetExplorerLeagueDto
            {
                Name = l.Name,
                Slug = l.Slug,
                DisplayName = l.DisplayName,
                FromCache = false,
                CachedAt = DateTime.UtcNow
            }).OrderBy(l => l.Name));
        })
        .WithName("GetBetExplorerLeagues")
        .WithDescription("Get available leagues from BetExplorer for a country. Results are cached for 24 hours.");
    }
}

public record BetExplorerLeagueDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool FromCache { get; init; }
    public DateTime? CachedAt { get; init; }
}
