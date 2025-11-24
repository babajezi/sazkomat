using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Scraper for Betano.cz betting provider
/// Extracts league data from window["initial_state"] JSON embedded in HTML
/// </summary>
public class BetanoScraper : IBettingProviderScraper
{
    private readonly BetanoJsonExtractor _jsonExtractor;
    private readonly ILogger<BetanoScraper> _logger;
    private const string BaseUrl = "https://www.betano.cz";

    public string ProviderCode => "betano";

    public BetanoScraper(
        BetanoJsonExtractor jsonExtractor,
        ILogger<BetanoScraper> logger)
    {
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    public async Task<Result<List<LeagueAvailability>>> GetAvailableLeaguesAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching available leagues from Betano for sport: {SportCode}", sportCode);

            // Map sport code to Betano URL
            var sportUrl = MapSportCodeToUrl(sportCode);
            if (sportUrl == null)
            {
                return Result<List<LeagueAvailability>>.Failure($"Sport code '{sportCode}' not supported by Betano scraper");
            }

            var fullUrl = $"{BaseUrl}{sportUrl}";
            _logger.LogInformation("Extracting data from: {Url}", fullUrl);

            // Extract JSON data from Betano page
            var extractResult = await _jsonExtractor.ExtractLeagueDataAsync(fullUrl);
            if (!extractResult.IsSuccess)
            {
                return Result<List<LeagueAvailability>>.Failure(extractResult.Error);
            }

            var betanoData = extractResult.Value;

            // Transform Betano data to LeagueAvailability entities
            var leagues = TransformToLeagueAvailability(betanoData, sportCode);

            _logger.LogInformation("Successfully extracted {Count} leagues from Betano for {SportCode}",
                leagues.Count, sportCode);

            return Result<List<LeagueAvailability>>.Success(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Betano for sport {SportCode}", sportCode);
            return Result<List<LeagueAvailability>>.Failure($"Failed to scrape Betano: {ex.Message}");
        }
    }

    /// <summary>
    /// Transforms Betano data structure to LeagueAvailability entities
    /// Flattens topLeagues and regionGroups.regions.leagues into a single list
    /// </summary>
    private List<LeagueAvailability> TransformToLeagueAvailability(
        Models.BetanoData betanoData,
        string sportCode)
    {
        var leagues = new List<LeagueAvailability>();

        // Add top leagues (featured leagues)
        foreach (var topLeague in betanoData.TopLeagues)
        {
            leagues.Add(new LeagueAvailability
            {
                ProviderLeagueName = topLeague.Name,
                ProviderLeagueId = topLeague.Id,
                ProviderUrl = $"{BaseUrl}{topLeague.Url}",
                SportCode = sportCode,
                CountryCode = topLeague.RegionCode,
                CountryName = topLeague.RegionName
            });
        }

        // Add leagues from region groups
        foreach (var regionGroup in betanoData.RegionGroups)
        {
            foreach (var region in regionGroup.Regions)
            {
                foreach (var league in region.Leagues)
                {
                    // Use league's region data if available, otherwise use parent region
                    var regionCode = league.RegionCode ?? region.RegionCode;
                    var regionName = league.RegionName ?? region.Name;

                    leagues.Add(new LeagueAvailability
                    {
                        ProviderLeagueName = league.Name,
                        ProviderLeagueId = league.Id,
                        ProviderUrl = $"{BaseUrl}{league.Url}",
                        SportCode = sportCode,
                        CountryCode = regionCode,
                        CountryName = regionName
                    });
                }
            }
        }

        // Remove duplicates (same league might appear in topLeagues and regionGroups)
        var uniqueLeagues = leagues
            .GroupBy(l => l.ProviderLeagueId)
            .Select(g => g.First())
            .ToList();

        _logger.LogDebug("Transformed {TotalCount} leagues, {UniqueCount} unique",
            leagues.Count, uniqueLeagues.Count);

        return uniqueLeagues;
    }

    /// <summary>
    /// Maps internal sport code to Betano sport URL
    /// </summary>
    private string? MapSportCodeToUrl(string sportCode)
    {
        return sportCode.ToLowerInvariant() switch
        {
            "football" => "/sport/fotbal/liga/",
            "basketball" => "/sport/basketbal/",
            "tennis" => "/sport/tenis/",
            "hockey" => "/sport/hokej/",
            _ => null
        };
    }
}
