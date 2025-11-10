using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using System.Text.RegularExpressions;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for enriching league metadata from betting providers with BetExplorer data.
/// Uses fuzzy matching to find leagues on BetExplorer and enrich with slug, priority, and other metadata.
/// </summary>
public class BetExplorerEnrichmentService : IBetExplorerEnrichmentService
{
    private readonly ILogger<BetExplorerEnrichmentService> _logger;
    private readonly BetExplorerLeagueMetadataScraper _betExplorerScraper;
    private readonly ILeagueNameMappingRepository _mappingRepository;

    // Cache to avoid repeated scraping of the same country
    private readonly Dictionary<string, List<LeagueMetadata>> _betExplorerLeaguesCache = new();

    public BetExplorerEnrichmentService(
        BetExplorerLeagueMetadataScraper betExplorerScraper,
        ILeagueNameMappingRepository mappingRepository,
        ILogger<BetExplorerEnrichmentService> logger)
    {
        _betExplorerScraper = betExplorerScraper;
        _mappingRepository = mappingRepository;
        _logger = logger;
    }

    public async Task<LeagueMetadata?> EnrichLeagueAsync(LeagueMetadata providerLeague, Country country, string providerCode)
    {
        try
        {
            _logger.LogInformation("Enriching league '{League}' from {Country} with BetExplorer data (provider: {Provider})",
                providerLeague.Name, country.Name, providerCode);

            // Get all BetExplorer leagues for this country (with caching)
            var betExplorerLeagues = await GetBetExplorerLeaguesAsync(country);

            if (!betExplorerLeagues.Any())
            {
                _logger.LogWarning("No BetExplorer leagues found for {Country}", country.Name);
                return null;
            }

            // Step 1: Try manual mapping from database first (highest priority)
            LeagueMetadata? match = null;
            var providerCodeLower = providerCode.ToLowerInvariant();
            var countryCodeLower = country.Code.ToLowerInvariant();

            // Query database for manual mapping
            var mapping = await _mappingRepository.FindMappingAsync(
                providerCodeLower,
                countryCodeLower,
                providerLeague.Name);

            if (mapping != null)
            {
                // Find league by slug in BetExplorer leagues
                match = betExplorerLeagues.FirstOrDefault(l =>
                    l.Slug.Equals(mapping.BetExplorerSlug, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    _logger.LogInformation("✓ Manual mapping found (DB): '{ProviderLeague}' → '{BetExplorerLeague}' (slug: {Slug})",
                        providerLeague.Name, match.Name, match.Slug);
                }
                else
                {
                    _logger.LogWarning("Manual mapping exists in DB for '{ProviderLeague}' → slug '{Slug}', but slug not found in BetExplorer leagues",
                        providerLeague.Name, mapping.BetExplorerSlug);
                }
            }

            // Step 2: If no manual mapping found, use automatic matching
            if (match == null)
            {
                match = FindBestMatch(providerLeague, betExplorerLeagues);
            }

            if (match == null)
            {
                _logger.LogWarning("No matching BetExplorer league found for '{League}' ({Country})",
                    providerLeague.Name, country.Name);
                return null;
            }

            _logger.LogInformation("Found BetExplorer match for '{ProviderLeague}': '{BetExplorerLeague}' (slug: {Slug})",
                providerLeague.Name, match.Name, match.Slug);

            // Create enriched metadata combining provider data with BetExplorer data
            var enriched = new LeagueMetadata
            {
                // Keep provider's original name and display name
                Name = providerLeague.Name,
                DisplayName = providerLeague.DisplayName,

                // Use BetExplorer's slug (critical for data import)
                Slug = match.Slug,

                // Keep provider's bettable status (true by definition since it came from betting provider)
                IsBettable = true,

                // Use BetExplorer's priority (order on their site)
                Priority = match.Priority,

                // Keep season info if present
                SeasonName = providerLeague.SeasonName,
                IsCurrentSeason = providerLeague.IsCurrentSeason
            };

            return enriched;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching league '{League}' ({Country})",
                providerLeague.Name, country.Name);
            return null;
        }
    }

    private async Task<List<LeagueMetadata>> GetBetExplorerLeaguesAsync(Country country)
    {
        // Cache key based on country code (assuming Football sport)
        var cacheKey = country.Code.ToLowerInvariant();

        // Check cache first
        if (_betExplorerLeaguesCache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Using cached BetExplorer leagues for {Country}", country.Name);
            return cached;
        }

        // Scrape from BetExplorer
        _logger.LogInformation("Scraping BetExplorer leagues for {Country}", country.Name);

        // Get sport entity (assume Football for now - can be parameterized later)
        var sport = new Sport { Code = "football", Name = "Football" };

        var leagues = await _betExplorerScraper.ScrapeLeaguesAsync(sport, country);

        // Cache the results
        _betExplorerLeaguesCache[cacheKey] = leagues;

        _logger.LogInformation("Cached {Count} BetExplorer leagues for {Country}", leagues.Count, country.Name);

        return leagues;
    }

    private LeagueMetadata? FindBestMatch(LeagueMetadata providerLeague, List<LeagueMetadata> betExplorerLeagues)
    {
        var normalizedProviderName = NormalizeName(providerLeague.Name);

        _logger.LogDebug("Searching for match: '{OriginalName}' (normalized: '{NormalizedName}')",
            providerLeague.Name, normalizedProviderName);

        // Try exact match first
        var exactMatch = betExplorerLeagues.FirstOrDefault(bl =>
            NormalizeName(bl.Name).Equals(normalizedProviderName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogDebug("Found exact match: '{Match}'", exactMatch.Name);
            return exactMatch;
        }

        // Try fuzzy match - find league with highest similarity score
        var matches = betExplorerLeagues
            .Select(bl => new
            {
                League = bl,
                Similarity = CalculateSimilarity(normalizedProviderName, NormalizeName(bl.Name))
            })
            .Where(m => m.Similarity >= 0.7) // Threshold: 70% similarity
            .OrderByDescending(m => m.Similarity)
            .ToList();

        if (matches.Any())
        {
            var bestMatch = matches.First();
            _logger.LogDebug("Found fuzzy match: '{Match}' (similarity: {Similarity:P})",
                bestMatch.League.Name, bestMatch.Similarity);
            return bestMatch.League;
        }

        // Try slug-based matching as last resort
        // Example: "Czech First League" → "1-liga"
        var providerSlugWords = GetSlugWords(normalizedProviderName);
        var slugMatch = betExplorerLeagues.FirstOrDefault(bl =>
        {
            var betExplorerSlugWords = GetSlugWords(bl.Slug);
            return providerSlugWords.Intersect(betExplorerSlugWords).Count() >= 2;
        });

        if (slugMatch != null)
        {
            _logger.LogDebug("Found slug-based match: '{Match}' (slug: {Slug})",
                slugMatch.Name, slugMatch.Slug);
            return slugMatch;
        }

        return null;
    }

    private string NormalizeName(string name)
    {
        // Remove special characters, convert to lowercase, normalize whitespace
        var normalized = Regex.Replace(name, @"[^\w\s-]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Remove common suffixes/prefixes
        normalized = Regex.Replace(normalized, @"\b(the|league|division|liga)\b", "", RegexOptions.IgnoreCase);

        return normalized.Trim().ToLowerInvariant();
    }

    private List<string> GetSlugWords(string text)
    {
        // Extract meaningful words for slug matching (numbers and words with 2+ chars)
        return Regex.Matches(text.ToLowerInvariant(), @"\b(\d+|[a-z]{2,})\b")
            .Select(m => m.Value)
            .ToList();
    }

    private double CalculateSimilarity(string str1, string str2)
    {
        // Levenshtein distance-based similarity
        int distance = LevenshteinDistance(str1, str2);
        int maxLength = Math.Max(str1.Length, str2.Length);

        if (maxLength == 0) return 1.0;

        return 1.0 - (double)distance / maxLength;
    }

    private int LevenshteinDistance(string str1, string str2)
    {
        int n = str1.Length;
        int m = str2.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
