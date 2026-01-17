using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Adapter that bridges KingsbetScraper to ILeagueMetadataScraper interface.
/// Enables Kingsbet to work with ScanService.
///
/// Kingsbet uses Altenar sportsbook which provides structured data with:
/// - Categories (countries) with ISO codes
/// - Championships (leagues) linked to categories via champIds
///
/// IMPORTANT: Uses caching to avoid repeated HTTP calls - single API request
/// returns all leagues for a sport, so we cache results for the duration of a scan.
/// </summary>
public class KingsbetLeagueMetadataScraper : ILeagueMetadataScraper, IUnmappedCountryLeagueProvider
{
    private readonly KingsbetScraper _kingsbetScraper;
    private readonly ILogger<KingsbetLeagueMetadataScraper> _logger;

    // Cache for scraped leagues - single HTTP request returns all leagues
    private List<LeagueAvailability>? _cachedLeagues;
    private string? _cachedSportCode;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private bool _fetchFailed;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    // Track leagues that couldn't be mapped to any country
    private List<LeagueAvailability>? _unmappedCountryLeagues;

    public KingsbetLeagueMetadataScraper(
        KingsbetScraper kingsbetScraper,
        ILogger<KingsbetLeagueMetadataScraper> logger)
    {
        _kingsbetScraper = kingsbetScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Code.Equals("kingsbet", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        ArgumentNullException.ThrowIfNull(sport);
        ArgumentNullException.ThrowIfNull(country);

        // Use cached leagues if available and not expired
        var allLeagues = await GetCachedLeaguesAsync(sport.Code);

        if (allLeagues == null || allLeagues.Count == 0)
        {
            _logger.LogDebug("No leagues available from Kingsbet for {Country}", country.Name);
            return new List<LeagueMetadata>();
        }

        // Filter by country code
        var leagues = allLeagues
            .Where(l => l.CountryCode != null &&
                       l.CountryCode.Equals(country.Code, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LeagueMetadata
            {
                Name = l.ProviderLeagueName,
                DisplayName = l.ProviderLeagueName, // Kingsbet names are display-friendly
                Slug = GenerateSlug(l.ProviderLeagueName),
                CountryCode = country.Code,
                ProviderLeagueId = l.ProviderLeagueId,
                Priority = 5,
                IsBettable = true,
                IsCurrentSeason = true // Kingsbet only shows current season
            })
            .ToList();

        if (leagues.Count > 0)
        {
            _logger.LogInformation("Found {Count} leagues in {Country} from Kingsbet cache", leagues.Count, country.Name);
        }

        return leagues;
    }

    /// <summary>
    /// Gets leagues from cache or fetches from Kingsbet if cache is empty/expired.
    /// This ensures we only make ONE HTTP request even when scanning many countries.
    /// </summary>
    private async Task<List<LeagueAvailability>?> GetCachedLeaguesAsync(string sportCode)
    {
        // Check if cache is valid
        if (_cachedLeagues != null &&
            _cachedSportCode == sportCode &&
            DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedLeagues;
        }

        // If we already tried and failed, don't retry for every country
        if (_fetchFailed && _cachedSportCode == sportCode)
        {
            _logger.LogDebug("Skipping Kingsbet fetch - previous attempt failed");
            return null;
        }

        // Fetch fresh data
        _logger.LogInformation("Fetching ALL leagues from Kingsbet for sport {Sport} (single API request)...", sportCode);
        var result = await _kingsbetScraper.GetAvailableLeaguesAsync(sportCode);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to scrape Kingsbet: {Error}", result.Error);
            _cachedLeagues = null;
            _cachedSportCode = sportCode;
            _fetchFailed = true;
            return null;
        }

        // Cache the results
        _cachedLeagues = result.Value;
        _cachedSportCode = sportCode;
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
        _fetchFailed = false;

        // Track leagues without country mapping (international competitions)
        _unmappedCountryLeagues = _cachedLeagues?
            .Where(l => string.IsNullOrEmpty(l.CountryCode))
            .ToList();

        if (_unmappedCountryLeagues?.Count > 0)
        {
            _logger.LogWarning("{Count} leagues are international competitions (no country mapping):", _unmappedCountryLeagues.Count);
            foreach (var league in _unmappedCountryLeagues.Take(10)) // Log first 10
            {
                _logger.LogWarning("  - '{LeagueName}' (CountryName: {CountryName})",
                    league.ProviderLeagueName, league.CountryName);
            }
            if (_unmappedCountryLeagues.Count > 10)
            {
                _logger.LogWarning("  ... and {More} more", _unmappedCountryLeagues.Count - 10);
            }
        }

        var mappedCount = (_cachedLeagues?.Count ?? 0) - (_unmappedCountryLeagues?.Count ?? 0);
        _logger.LogInformation("Cached {Total} leagues from Kingsbet ({Mapped} mapped to countries, {Unmapped} international)",
            _cachedLeagues?.Count ?? 0, mappedCount, _unmappedCountryLeagues?.Count ?? 0);

        return _cachedLeagues;
    }

    public Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        // Kingsbet only shows current season leagues
        return ScrapeLeaguesAsync(sport, country);
    }

    /// <summary>
    /// Returns leagues that couldn't be mapped to any country.
    /// For Kingsbet, these are international competitions (Europa League, Champions League, etc.)
    /// </summary>
    public List<UnmappedCountryLeague> GetUnmappedCountryLeagues()
    {
        return _unmappedCountryLeagues?
            .Select(l => new UnmappedCountryLeague
            {
                ProviderLeagueId = l.ProviderLeagueId ?? string.Empty,
                ProviderLeagueName = l.ProviderLeagueName,
                ProviderUrl = l.ProviderUrl
            })
            .ToList() ?? new List<UnmappedCountryLeague>();
    }

    private static string GenerateSlug(string name)
    {
        return name
            .ToLowerInvariant()
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("'", "", StringComparison.Ordinal)
            .Replace("–", "-", StringComparison.Ordinal); // Czech en-dash
    }
}
