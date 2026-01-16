using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Scraper for Fortuna.cz betting provider.
/// Extracts league data from the Fortuna football page.
///
/// Note: Fortuna uses Czech country names in URLs (e.g., "anglie", "nemecko").
/// Country code normalization is handled by FortunaJsonExtractor.NormalizeCountryCode().
/// </summary>
public class FortunaScraper : IBettingProviderScraper
{
    private readonly FortunaJsonExtractor _jsonExtractor;
    private readonly ILogger<FortunaScraper> _logger;
    private const string BaseUrl = "https://www.ifortuna.cz";

    public string ProviderCode => "fortuna";

    public FortunaScraper(
        FortunaJsonExtractor jsonExtractor,
        ILogger<FortunaScraper> logger)
    {
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    public async Task<Result<List<LeagueAvailability>>> GetAvailableLeaguesAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching available leagues from Fortuna for sport: {SportCode}", sportCode);

            // Map sport code to Fortuna URL
            var sportUrl = MapSportCodeToUrl(sportCode);
            if (sportUrl == null)
            {
                return Result<List<LeagueAvailability>>.Failure(
                    $"Sport code '{sportCode}' not supported by Fortuna scraper (only 'football' is currently supported)");
            }

            var fullUrl = $"{BaseUrl}{sportUrl}";
            _logger.LogInformation("Extracting data from: {Url}", fullUrl);

            // Extract data from Fortuna page
            var extractResult = await _jsonExtractor.ExtractLeagueDataAsync(fullUrl);
            if (!extractResult.IsSuccess)
            {
                return Result<List<LeagueAvailability>>.Failure(extractResult.Error);
            }

            var fortunaData = extractResult.Value;

            // Transform Fortuna data to LeagueAvailability entities
            var leagues = TransformToLeagueAvailability(fortunaData, sportCode);

            _logger.LogInformation("Successfully extracted {Count} leagues from Fortuna for {SportCode}",
                leagues.Count, sportCode);

            return Result<List<LeagueAvailability>>.Success(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Fortuna for sport {SportCode}", sportCode);
            return Result<List<LeagueAvailability>>.Failure($"Failed to scrape Fortuna: {ex.Message}");
        }
    }

    /// <summary>
    /// Transforms Fortuna data structure to LeagueAvailability entities.
    /// Flattens country groups and their leagues into a single list.
    /// </summary>
    private List<LeagueAvailability> TransformToLeagueAvailability(
        Models.FortunaData fortunaData,
        string sportCode)
    {
        var leagues = new List<LeagueAvailability>();

        foreach (var countryGroup in fortunaData.CountryGroups)
        {
            // Normalize country code from Czech slug to English
            var normalizedCountryCode = FortunaJsonExtractor.NormalizeCountryCode(countryGroup.Code ?? countryGroup.Name);

            foreach (var league in countryGroup.Leagues)
            {
                leagues.Add(new LeagueAvailability
                {
                    ProviderLeagueName = league.Name,
                    ProviderLeagueId = league.LeagueId ?? ExtractLeagueIdFromUrl(league.Url),
                    ProviderUrl = league.Url.StartsWith("http") ? league.Url : $"{BaseUrl}{league.Url}",
                    SportCode = sportCode,
                    CountryCode = normalizedCountryCode,
                    CountryName = FormatCountryName(countryGroup.Name)
                });
            }
        }

        // Remove duplicates by URL
        var uniqueLeagues = leagues
            .GroupBy(l => l.ProviderUrl)
            .Select(g => g.First())
            .ToList();

        _logger.LogDebug("Transformed {TotalCount} leagues, {UniqueCount} unique",
            leagues.Count, uniqueLeagues.Count);

        return uniqueLeagues;
    }

    /// <summary>
    /// Extracts league ID from URL.
    /// E.g., "/sazeni/fotbal/anglie/premier-league" -> "premier-league"
    /// </summary>
    private static string ExtractLeagueIdFromUrl(string url)
    {
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : url;
    }

    /// <summary>
    /// Formats country name for display.
    /// Capitalizes first letter and converts dashes to spaces.
    /// E.g., "anglie" -> "Anglie", "bosna-a-hercegovina" -> "Bosna a Hercegovina"
    /// </summary>
    private static string FormatCountryName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Replace dashes with spaces
        var formatted = name.Replace("-", " ");

        // Capitalize first letter of each word
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());
    }

    /// <summary>
    /// Maps internal sport code to Fortuna URL path.
    /// Currently only football is supported.
    /// </summary>
    private static string? MapSportCodeToUrl(string sportCode)
    {
        return sportCode.ToLowerInvariant() switch
        {
            "football" => "/sazeni/fotbal?tab=matches&filter=all",
            // Future sport support can be added here:
            // "hockey" => "/sazeni/hokej",
            // "basketball" => "/sazeni/basketbal",
            // "tennis" => "/sazeni/tenis",
            _ => null
        };
    }

    /// <summary>
    /// Gets all available regions (countries) from Fortuna for a sport.
    /// </summary>
    public async Task<Result<List<RegionInfo>>> GetAvailableRegionsAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching all regions from Fortuna for sport: {SportCode}", sportCode);

            var sportUrl = MapSportCodeToUrl(sportCode);
            if (sportUrl == null)
            {
                return Result<List<RegionInfo>>.Failure(
                    $"Sport code '{sportCode}' not supported by Fortuna scraper");
            }

            var fullUrl = $"{BaseUrl}{sportUrl}";
            var extractResult = await _jsonExtractor.ExtractLeagueDataAsync(fullUrl);
            if (!extractResult.IsSuccess)
            {
                return Result<List<RegionInfo>>.Failure(extractResult.Error);
            }

            var fortunaData = extractResult.Value;

            // Extract unique regions from country groups
            var regions = fortunaData.CountryGroups
                .Select(cg => new RegionInfo
                {
                    Code = FortunaJsonExtractor.NormalizeCountryCode(cg.Code ?? cg.Name),
                    Name = FormatCountryName(cg.Name),
                    HasLeagues = cg.Leagues.Count > 0
                })
                .GroupBy(r => r.Code)
                .Select(g => g.First())
                .OrderBy(r => r.Name)
                .ToList();

            _logger.LogInformation("Found {Count} unique regions from Fortuna for {SportCode}",
                regions.Count, sportCode);

            return Result<List<RegionInfo>>.Success(regions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regions from Fortuna for sport {SportCode}", sportCode);
            return Result<List<RegionInfo>>.Failure($"Failed to get Fortuna regions: {ex.Message}");
        }
    }
}
