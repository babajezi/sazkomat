using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;
using Sazkomat.Data.Services;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Fortuna league metadata scraper.
/// Scrapes league data from Fortuna.cz for a specific country.
/// Uses FortunaScraper internally and caches results per sport to avoid repeated HTTP requests.
/// </summary>
public class FortunaLeagueMetadataScraper : ILeagueMetadataScraper, IUnmappedCountryLeagueProvider
{
    private readonly FortunaScraper _fortunaScraper;
    private readonly ILogger<FortunaLeagueMetadataScraper> _logger;

    // Cache for all leagues (one HTTP request fetches all leagues, then we filter)
    private List<LeagueAvailability>? _cachedLeagues;
    private string? _cachedSportCode;
    private bool _fetchFailed = false;
    private readonly object _cacheLock = new();

    public FortunaLeagueMetadataScraper(
        FortunaScraper fortunaScraper,
        ILogger<FortunaLeagueMetadataScraper> logger)
    {
        _fortunaScraper = fortunaScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("fortuna", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        var sportCode = MapSportToCode(sport);
        if (sportCode == null)
        {
            _logger.LogWarning("Sport {SportName} not supported by Fortuna scraper", sport.Name);
            return new List<LeagueMetadata>();
        }

        // Get all leagues (cached or fetch)
        var allLeagues = await GetAllLeaguesAsync(sportCode);
        if (allLeagues == null)
        {
            return new List<LeagueMetadata>();
        }

        // Filter by country
        var leagues = allLeagues
            .Where(l => l.CountryCode != null &&
                       l.CountryCode.Equals(country.Code, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LeagueMetadata
            {
                Name = l.ProviderLeagueName,
                DisplayName = l.ProviderLeagueName,
                Slug = l.ProviderLeagueId ?? ExtractSlugFromUrl(l.ProviderUrl),
                CountryCode = l.CountryCode,
                ProviderLeagueId = l.ProviderLeagueId,
                Priority = 5,
                IsBettable = true,
                IsCurrentSeason = true
            })
            .ToList();

        _logger.LogDebug("Found {Count} leagues for country {Country} from Fortuna",
            leagues.Count, country.Code);

        return leagues;
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        // Fortuna only shows current season, so this is the same as ScrapeLeaguesAsync
        return await ScrapeLeaguesAsync(sport, country);
    }

    /// <summary>
    /// Gets all leagues for a sport, using cache if available.
    /// Single HTTP request fetches all leagues, then we filter by country.
    /// </summary>
    private async Task<List<LeagueAvailability>?> GetAllLeaguesAsync(string sportCode)
    {
        lock (_cacheLock)
        {
            // Return cached if available and same sport
            if (_cachedLeagues != null && _cachedSportCode == sportCode)
            {
                _logger.LogDebug("Using cached Fortuna leagues for sport {SportCode}", sportCode);
                return _cachedLeagues;
            }

            // If we already tried and failed, don't retry for every country
            if (_fetchFailed && _cachedSportCode == sportCode)
            {
                _logger.LogDebug("Skipping Fortuna fetch - previous attempt failed");
                return null;
            }
        }

        // Fetch fresh data
        _logger.LogInformation("Fetching all Fortuna leagues for sport {SportCode}", sportCode);

        var result = await _fortunaScraper.GetAvailableLeaguesAsync(sportCode);

        lock (_cacheLock)
        {
            _cachedSportCode = sportCode;

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to fetch Fortuna leagues: {Error}", result.Error);
                _fetchFailed = true;
                return null;
            }

            _cachedLeagues = result.Value;
            _fetchFailed = false;

            _logger.LogInformation("Cached {Count} leagues from Fortuna for sport {SportCode}",
                _cachedLeagues.Count, sportCode);

            return _cachedLeagues;
        }
    }

    /// <summary>
    /// Maps Sport entity to sport code.
    /// </summary>
    private static string? MapSportToCode(Sport sport)
    {
        return sport.Code?.ToLowerInvariant() switch
        {
            "football" => "football",
            "fotbal" => "football",
            // Add more sports as needed
            _ => null
        };
    }

    /// <summary>
    /// Extracts slug from URL path.
    /// </summary>
    private static string ExtractSlugFromUrl(string url)
    {
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : url;
    }

    #region IUnmappedCountryLeagueProvider

    /// <summary>
    /// Returns leagues that couldn't be mapped to a country.
    /// Useful for identifying missing country mappings.
    /// </summary>
    public List<UnmappedCountryLeague> GetUnmappedCountryLeagues()
    {
        if (_cachedLeagues == null)
            return new List<UnmappedCountryLeague>();

        return _cachedLeagues
            .Where(l => string.IsNullOrEmpty(l.CountryCode))
            .Select(l => new UnmappedCountryLeague
            {
                ProviderLeagueId = l.ProviderLeagueId ?? "",
                ProviderLeagueName = l.ProviderLeagueName,
                ProviderUrl = l.ProviderUrl
            })
            .ToList();
    }

    #endregion
}
