using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Adapter that bridges TipsportScraper to ILeagueMetadataScraper interface
/// Enables Tipsport to work with ScanService
///
/// Note: Tipsport doesn't have country hierarchy - it uses Czech names like "1. anglická liga".
/// The country is derived from the competition title, not from API structure.
///
/// IMPORTANT: Uses caching to avoid repeated HTTP calls - Tipsport returns ALL leagues
/// in a single HTTP request, so we cache the results for the duration of a scan.
/// </summary>
public class TipsportLeagueMetadataScraper : ILeagueMetadataScraper, IUnmappedCountryLeagueProvider
{
    private readonly TipsportScraper _tipsportScraper;
    private readonly ILogger<TipsportLeagueMetadataScraper> _logger;

    // Cache for scraped leagues - single HTTP request returns all leagues
    private List<LeagueAvailability>? _cachedLeagues;
    private string? _cachedSportCode;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private bool _fetchFailed = false;  // Track if fetch failed to avoid repeated attempts
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    // Track leagues that couldn't be mapped to any country
    private List<LeagueAvailability>? _unmappedCountryLeagues;

    public TipsportLeagueMetadataScraper(
        TipsportScraper tipsportScraper,
        ILogger<TipsportLeagueMetadataScraper> logger)
    {
        _tipsportScraper = tipsportScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("tipsport", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<LeagueMetadata>> ScrapeLeaguesAsync(Sport sport, Country country)
    {
        // Use cached leagues if available and not expired
        var allLeagues = await GetCachedLeaguesAsync(sport.Code);

        if (allLeagues == null || allLeagues.Count == 0)
        {
            _logger.LogDebug("No leagues available from Tipsport for {Country}", country.Name);
            return new List<LeagueMetadata>();
        }

        // Filter by country - Tipsport derives country from Czech competition name
        // The CountryCode is set by TipsportScraper.DeriveCountryFromTitle()
        var leagues = allLeagues
            .Where(l => l.CountryCode != null &&
                       l.CountryCode.Equals(country.Code, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LeagueMetadata
            {
                Name = l.ProviderLeagueName,
                DisplayName = l.ProviderLeagueName, // Tipsport names are already display-friendly Czech
                Slug = GenerateSlug(l.ProviderLeagueName),
                CountryCode = country.Code,
                ProviderLeagueId = l.ProviderLeagueId,
                Priority = 5,
                IsBettable = true,
                IsCurrentSeason = true // Tipsport only shows current season
            })
            .ToList();

        if (leagues.Count > 0)
        {
            _logger.LogInformation("Found {Count} leagues in {Country} from Tipsport cache", leagues.Count, country.Name);
        }
        return leagues;
    }

    /// <summary>
    /// Gets leagues from cache or fetches from Tipsport if cache is empty/expired.
    /// This ensures we only make ONE HTTP request even when scanning 88+ countries.
    /// If the initial fetch fails, subsequent calls will return null immediately
    /// instead of retrying 88 times.
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
            _logger.LogDebug("Skipping Tipsport fetch - previous attempt failed");
            return null;
        }

        // Fetch fresh data
        _logger.LogInformation("Fetching ALL leagues from Tipsport for sport {Sport} (single HTTP request)...", sportCode);
        var result = await _tipsportScraper.GetAvailableLeaguesAsync(sportCode);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to scrape Tipsport: {Error}", result.Error);
            _cachedLeagues = null;
            _cachedSportCode = sportCode;
            _fetchFailed = true;  // Mark as failed to avoid retrying for each country
            return null;
        }

        // Cache the results
        _cachedLeagues = result.Value;
        _cachedSportCode = sportCode;
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
        _fetchFailed = false;  // Reset failure flag on success

        // Track leagues without country mapping
        _unmappedCountryLeagues = _cachedLeagues?
            .Where(l => string.IsNullOrEmpty(l.CountryCode))
            .ToList();

        if (_unmappedCountryLeagues?.Count > 0)
        {
            _logger.LogWarning("⚠ {Count} leagues could not be mapped to any country:", _unmappedCountryLeagues.Count);
            foreach (var league in _unmappedCountryLeagues)
            {
                _logger.LogWarning("  - '{LeagueName}' (no country mapping in dictionary)", league.ProviderLeagueName);
            }
        }

        var mappedCount = (_cachedLeagues?.Count ?? 0) - (_unmappedCountryLeagues?.Count ?? 0);
        _logger.LogInformation("Cached {Total} leagues from Tipsport ({Mapped} mapped, {Unmapped} unmapped)",
            _cachedLeagues?.Count ?? 0, mappedCount, _unmappedCountryLeagues?.Count ?? 0);

        return _cachedLeagues;
    }

    public Task<List<LeagueMetadata>> ScrapeLeaguesForCurrentSeasonAsync(
        Sport sport,
        Country country,
        List<string> seasonPatterns)
    {
        // Tipsport only shows current season leagues, so this is the same as ScrapeLeaguesAsync
        return ScrapeLeaguesAsync(sport, country);
    }

    /// <summary>
    /// Returns leagues that couldn't be mapped to any country.
    /// These leagues have Czech names that don't match any entry in the country dictionary.
    /// Call this after ScrapeLeaguesAsync to get the unmapped leagues for saving.
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
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace("'", "");
    }
}
