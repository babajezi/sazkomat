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
    /// Gets all available regions (countries) from Betano for a sport.
    /// Unlike GetAvailableLeaguesAsync, this returns ALL regions even if they have no leagues.
    /// </summary>
    public async Task<Result<List<RegionInfo>>> GetAvailableRegionsAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching all regions from Betano for sport: {SportCode}", sportCode);

            var sportUrl = MapSportCodeToUrl(sportCode);
            if (sportUrl == null)
            {
                return Result<List<RegionInfo>>.Failure($"Sport code '{sportCode}' not supported by Betano scraper");
            }

            var fullUrl = $"{BaseUrl}{sportUrl}";
            var extractResult = await _jsonExtractor.ExtractLeagueDataAsync(fullUrl);
            if (!extractResult.IsSuccess)
            {
                return Result<List<RegionInfo>>.Failure(extractResult.Error);
            }

            var betanoData = extractResult.Value;
            var regions = new List<RegionInfo>();

            // Extract unique regions from topLeagues
            foreach (var topLeague in betanoData.TopLeagues)
            {
                if (!string.IsNullOrEmpty(topLeague.RegionCode) && !string.IsNullOrEmpty(topLeague.RegionName))
                {
                    regions.Add(new RegionInfo
                    {
                        Code = topLeague.RegionCode,
                        Name = topLeague.RegionName,
                        HasLeagues = true
                    });
                }
            }

            // Extract ALL regions from regionGroups - including those without leagues
            foreach (var regionGroup in betanoData.RegionGroups)
            {
                foreach (var region in regionGroup.Regions)
                {
                    if (!string.IsNullOrEmpty(region.Name))
                    {
                        // Some countries have regionCode="default" (e.g., Bosnia, Ireland, etc.)
                        // In that case, derive code from URL or use normalized name
                        var regionCode = region.RegionCode;
                        if (string.IsNullOrEmpty(regionCode) || regionCode == "default")
                        {
                            // Try to extract country code from URL (e.g., "/sport/fotbal/souteze/bosna-a-hercegovina/11478/")
                            regionCode = ExtractCountryCodeFromUrl(region.Url) ?? NormalizeToCode(region.Name);
                        }

                        regions.Add(new RegionInfo
                        {
                            Code = regionCode,
                            Name = region.Name,
                            HasLeagues = region.Leagues.Count > 0
                        });
                    }
                }
            }

            // Deduplicate by code (use Name as fallback for uniqueness)
            var uniqueRegions = regions
                .GroupBy(r => r.Code)
                .Select(g => g.First())
                .OrderBy(r => r.Name)
                .ToList();

            _logger.LogInformation("Found {Count} unique regions from Betano for {SportCode}",
                uniqueRegions.Count, sportCode);

            return Result<List<RegionInfo>>.Success(uniqueRegions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regions from Betano for sport {SportCode}", sportCode);
            return Result<List<RegionInfo>>.Failure($"Failed to get Betano regions: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps internal sport code to Betano sport URL
    /// </summary>
    private static string? MapSportCodeToUrl(string sportCode)
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

    /// <summary>
    /// Extracts country code from Betano URL.
    /// E.g., "/sport/fotbal/souteze/bosna-a-hercegovina/11478/" -> "bosna-a-hercegovina"
    /// </summary>
    private static string? ExtractCountryCodeFromUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // URL format: /sport/fotbal/souteze/{country-code}/{id}/
        // or: /sport/fotbal/{country-code}/{league}/
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Look for the country part after "souteze" or "liga"
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "souteze" || parts[i] == "liga")
            {
                var potentialCode = parts[i + 1];
                // Skip if it's a numeric ID
                if (!int.TryParse(potentialCode, out _))
                {
                    return potentialCode;
                }
            }
        }

        // Fallback: try to find a non-sport, non-numeric part
        foreach (var part in parts)
        {
            if (part != "sport" && part != "fotbal" && part != "basketbal" &&
                part != "tenis" && part != "hokej" && part != "souteze" && part != "liga" &&
                !int.TryParse(part, out _))
            {
                return part;
            }
        }

        return null;
    }

    /// <summary>
    /// Normalizes a country name to a URL-friendly code.
    /// E.g., "Bosna a Hercegovina" -> "bosna-a-hercegovina"
    /// </summary>
    private static string NormalizeToCode(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á", "a")
            .Replace("č", "c")
            .Replace("ď", "d")
            .Replace("é", "e")
            .Replace("ě", "e")
            .Replace("í", "i")
            .Replace("ň", "n")
            .Replace("ó", "o")
            .Replace("ř", "r")
            .Replace("š", "s")
            .Replace("ť", "t")
            .Replace("ú", "u")
            .Replace("ů", "u")
            .Replace("ý", "y")
            .Replace("ž", "z");
    }

    /// <summary>
    /// Gets both regions (countries) AND leagues in a single HTTP request.
    /// Optimized for combined scan to avoid duplicate fetches.
    /// </summary>
    public async Task<Result<BetanoFullScanResult>> GetFullDataAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching full data (regions + leagues) from Betano for sport: {SportCode}", sportCode);

            var sportUrl = MapSportCodeToUrl(sportCode);
            if (sportUrl == null)
            {
                return Result<BetanoFullScanResult>.Failure($"Sport code '{sportCode}' not supported by Betano scraper");
            }

            var fullUrl = $"{BaseUrl}{sportUrl}";
            _logger.LogInformation("Extracting full data from: {Url}", fullUrl);

            // Single HTTP request
            var extractResult = await _jsonExtractor.ExtractLeagueDataAsync(fullUrl);
            if (!extractResult.IsSuccess)
            {
                return Result<BetanoFullScanResult>.Failure(extractResult.Error);
            }

            var betanoData = extractResult.Value;

            // Extract regions (same logic as GetAvailableRegionsAsync)
            var regions = ExtractRegions(betanoData);

            // Extract leagues (same logic as GetAvailableLeaguesAsync)
            var leagues = TransformToLeagueAvailability(betanoData, sportCode);

            _logger.LogInformation("Full scan extracted {RegionCount} regions and {LeagueCount} leagues from Betano",
                regions.Count, leagues.Count);

            return Result<BetanoFullScanResult>.Success(new BetanoFullScanResult
            {
                Regions = regions,
                Leagues = leagues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting full data from Betano for sport {SportCode}", sportCode);
            return Result<BetanoFullScanResult>.Failure($"Failed to get Betano full data: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts unique regions from Betano data structure.
    /// </summary>
    private List<RegionInfo> ExtractRegions(Models.BetanoData betanoData)
    {
        var regions = new List<RegionInfo>();

        // Extract unique regions from topLeagues
        foreach (var topLeague in betanoData.TopLeagues)
        {
            if (!string.IsNullOrEmpty(topLeague.RegionCode) && !string.IsNullOrEmpty(topLeague.RegionName))
            {
                regions.Add(new RegionInfo
                {
                    Code = topLeague.RegionCode,
                    Name = topLeague.RegionName,
                    HasLeagues = true
                });
            }
        }

        // Extract ALL regions from regionGroups - including those without leagues
        foreach (var regionGroup in betanoData.RegionGroups)
        {
            foreach (var region in regionGroup.Regions)
            {
                if (!string.IsNullOrEmpty(region.Name))
                {
                    var regionCode = region.RegionCode;
                    if (string.IsNullOrEmpty(regionCode) || regionCode == "default")
                    {
                        regionCode = ExtractCountryCodeFromUrl(region.Url) ?? NormalizeToCode(region.Name);
                    }

                    regions.Add(new RegionInfo
                    {
                        Code = regionCode,
                        Name = region.Name,
                        HasLeagues = region.Leagues.Count > 0
                    });
                }
            }
        }

        // Deduplicate by code
        return regions
            .GroupBy(r => r.Code)
            .Select(g => g.First())
            .OrderBy(r => r.Name)
            .ToList();
    }
}

/// <summary>
/// Region info from Betano
/// </summary>
public class RegionInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasLeagues { get; set; }
}

/// <summary>
/// Result of full Betano scan containing both regions and leagues.
/// </summary>
public class BetanoFullScanResult
{
    public List<RegionInfo> Regions { get; set; } = new();
    public List<LeagueAvailability> Leagues { get; set; } = new();
}
