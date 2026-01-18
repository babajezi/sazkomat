using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Helpers;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Validators;
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
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILeagueProviderRepository _leagueProviderRepository;
    private readonly IDataProviderRepository _dataProviderRepository;
    private readonly ILeagueRoundValidator _roundValidator;

    // Cache to avoid repeated scraping of the same country
    private readonly Dictionary<string, List<LeagueMetadata>> _betExplorerLeaguesCache = new();

    public BetExplorerEnrichmentService(
        BetExplorerLeagueMetadataScraper betExplorerScraper,
        ILeagueNameMappingRepository mappingRepository,
        ILeagueRepository leagueRepository,
        ILeagueProviderRepository leagueProviderRepository,
        IDataProviderRepository dataProviderRepository,
        ILeagueRoundValidator roundValidator,
        ILogger<BetExplorerEnrichmentService> logger)
    {
        _betExplorerScraper = betExplorerScraper;
        _mappingRepository = mappingRepository;
        _leagueRepository = leagueRepository;
        _leagueProviderRepository = leagueProviderRepository;
        _dataProviderRepository = dataProviderRepository;
        _roundValidator = roundValidator;
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

            // Query database for manual mapping (with fallback to global rules)
            var mapping = await _mappingRepository.FindMappingWithFallbackAsync(
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

                // Keep provider's country code
                CountryCode = providerLeague.CountryCode,

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

        // Detect if this is a women's league from provider
        bool isWomensLeague = IsWomensLeague(providerLeague.Name);

        _logger.LogDebug("Searching for match: '{OriginalName}' (normalized: '{NormalizedName}', women: {IsWomen})",
            providerLeague.Name, normalizedProviderName, isWomensLeague);

        // Filter BetExplorer leagues by gender first
        var genderFilteredLeagues = betExplorerLeagues
            .Where(bl => IsWomensLeague(bl.Name) == isWomensLeague)
            .ToList();

        _logger.LogDebug("After gender filter: {Count} leagues (from {Total})",
            genderFilteredLeagues.Count, betExplorerLeagues.Count);

        // If no leagues match gender, don't match at all
        if (!genderFilteredLeagues.Any())
        {
            _logger.LogDebug("No leagues match gender filter, skipping");
            return null;
        }

        // Try exact match first (on gender-filtered list)
        var exactMatch = genderFilteredLeagues.FirstOrDefault(bl =>
            NormalizeName(bl.Name).Equals(normalizedProviderName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
        {
            _logger.LogDebug("Found exact match: '{Match}'", exactMatch.Name);
            return exactMatch;
        }

        // DISABLED: Fuzzy matching removed due to unreliable results (e.g., "1. turecká liga" → "1. Lig" instead of "Super Lig")
        // Leagues that don't match exactly will go to unmatched_leagues for manual resolution
        // var matches = genderFilteredLeagues
        //     .Select(bl => new
        //     {
        //         League = bl,
        //         Similarity = CalculateSimilarity(normalizedProviderName, NormalizeName(bl.Name))
        //     })
        //     .Where(m => m.Similarity >= 0.7) // Threshold: 70% similarity
        //     .OrderByDescending(m => m.Similarity)
        //     .ToList();
        //
        // if (matches.Any())
        // {
        //     var bestMatch = matches.First();
        //     _logger.LogDebug("Found fuzzy match: '{Match}' (similarity: {Similarity:P})",
        //         bestMatch.League.Name, bestMatch.Similarity);
        //     return bestMatch.League;
        // }

        // Try slug-based matching as last resort
        // Example: "Czech First League" → "1-liga"
        var providerSlugWords = GetSlugWords(normalizedProviderName);
        var slugMatch = genderFilteredLeagues.FirstOrDefault(bl =>
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

    /// <summary>
    /// Detects if a league name indicates a women's competition.
    /// Betano uses "(Ž)" suffix, BetExplorer uses "Women" suffix.
    /// </summary>
    private bool IsWomensLeague(string leagueName)
    {
        if (string.IsNullOrEmpty(leagueName))
            return false;

        // Betano format: "Serie A (Ž)", "FA Cup (Ž)"
        if (leagueName.Contains("(Ž)") || leagueName.Contains("(ž)"))
            return true;

        // BetExplorer format: "Serie A Women", "FA Cup Women"
        if (leagueName.EndsWith(" Women", StringComparison.OrdinalIgnoreCase))
            return true;

        // Other common patterns
        if (leagueName.Contains("Women", StringComparison.OrdinalIgnoreCase) ||
            leagueName.Contains("Feminin", StringComparison.OrdinalIgnoreCase) ||
            leagueName.Contains("Femenin", StringComparison.OrdinalIgnoreCase) ||
            leagueName.Contains("Frauen", StringComparison.OrdinalIgnoreCase) ||
            leagueName.Contains("Damer", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
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

    /// <summary>
    /// Finds a matching league in the configuration database (BetExplorer source of truth).
    /// This is used by betting providers to create LeagueProvider mappings.
    /// </summary>
    public async Task<League?> FindMatchingLeagueAsync(LeagueMetadata providerLeague, Country country, string providerCode)
    {
        try
        {
            _logger.LogDebug("Finding matching config league for '{League}' from {Country} (provider: {Provider})",
                providerLeague.Name, country.Name, providerCode);

            var providerCodeLower = providerCode.ToLowerInvariant();
            var countryCodeLower = country.Code.ToLowerInvariant();

            // Detect gender BEFORE searching - critical for correct matching
            bool isWomensLeague = IsWomensLeague(providerLeague.Name);
            _logger.LogDebug("Gender detection for '{League}': isWomens={IsWomens}",
                providerLeague.Name, isWomensLeague);

            // Step 1: Try manual mapping from database first (with fallback to global rules)
            var mapping = await _mappingRepository.FindMappingWithFallbackAsync(
                providerCodeLower,
                countryCodeLower,
                providerLeague.Name);

            if (mapping != null)
            {
                // Find league by BetExplorer slug in configuration
                var mappedLeague = await _leagueRepository.GetByBetExplorerSlugAsync(mapping.BetExplorerSlug);
                if (mappedLeague != null)
                {
                    _logger.LogInformation("✓ Found league via manual mapping: '{ProviderLeague}' → '{ConfigLeague}' (slug: {Slug})",
                        providerLeague.Name, mappedLeague.Name, mappedLeague.BetExplorerSlug);
                    return mappedLeague;
                }
                else
                {
                    _logger.LogWarning("Manual mapping exists for '{ProviderLeague}' → slug '{Slug}', but no league found with that slug",
                        providerLeague.Name, mapping.BetExplorerSlug);
                }
            }

            // Step 2: Get all leagues for this country and filter by gender
            var countryLeagues = await _leagueRepository.GetByCountryIdAsync(country.Id);

            if (!countryLeagues.Any())
            {
                _logger.LogDebug("No leagues in configuration for country {Country}", country.Name);
                return null;
            }

            // Filter by gender first - women's leagues should only match women's leagues
            var genderFilteredLeagues = countryLeagues
                .Where(l => IsWomensLeague(l.Name) == isWomensLeague)
                .ToList();

            _logger.LogDebug("After gender filter: {Count} leagues (from {Total}) for '{League}'",
                genderFilteredLeagues.Count, countryLeagues.Count, providerLeague.Name);

            if (!genderFilteredLeagues.Any())
            {
                _logger.LogDebug("No leagues match gender filter for '{ProviderLeague}' [{Country}]",
                    providerLeague.Name, country.Name);
                return null;
            }

            // Try exact name match (on gender-filtered list)
            var exactMatch = genderFilteredLeagues.FirstOrDefault(l =>
                l.Name.Equals(providerLeague.Name, StringComparison.OrdinalIgnoreCase) ||
                (l.NameCs != null && l.NameCs.Equals(providerLeague.Name, StringComparison.OrdinalIgnoreCase)));

            if (exactMatch != null)
            {
                _logger.LogInformation("✓ Found league via exact name match: '{ProviderLeague}' → '{ConfigLeague}'",
                    providerLeague.Name, exactMatch.Name);
                return exactMatch;
            }

            // Try slug-based match (on gender-filtered list)
            var normalizedProviderName = NormalizeName(providerLeague.Name);
            var slugMatch = genderFilteredLeagues.FirstOrDefault(l =>
            {
                var normalizedConfigName = NormalizeName(l.Name);
                return normalizedConfigName.Equals(normalizedProviderName, StringComparison.OrdinalIgnoreCase);
            });

            if (slugMatch != null)
            {
                _logger.LogInformation("✓ Found league via normalized name match: '{ProviderLeague}' → '{ConfigLeague}'",
                    providerLeague.Name, slugMatch.Name);
                return slugMatch;
            }

            // DISABLED: Fuzzy matching removed due to unreliable results (e.g., "1. turecká liga" → "1. Lig" instead of "Super Lig")
            // Leagues that don't match exactly will go to unmatched_leagues for manual resolution
            // var fuzzyMatches = genderFilteredLeagues
            //     .Select(l => new
            //     {
            //         League = l,
            //         Similarity = CalculateSimilarity(normalizedProviderName, NormalizeName(l.Name))
            //     })
            //     .Where(m => m.Similarity >= 0.7)
            //     .OrderByDescending(m => m.Similarity)
            //     .ToList();
            //
            // if (fuzzyMatches.Any())
            // {
            //     var bestMatch = fuzzyMatches.First();
            //     _logger.LogInformation("✓ Found league via fuzzy match: '{ProviderLeague}' → '{ConfigLeague}' (similarity: {Similarity:P})",
            //         providerLeague.Name, bestMatch.League.Name, bestMatch.Similarity);
            //     return bestMatch.League;
            // }

            _logger.LogDebug("No matching config league found for '{ProviderLeague}' [{Country}]",
                providerLeague.Name, country.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding matching league for '{League}' ({Country})",
                providerLeague.Name, country.Name);
            return null;
        }
    }

    /// <summary>
    /// Finds an existing league or creates a new one from BetExplorer data.
    /// This is the main method for betting provider workflow.
    /// </summary>
    public async Task<League?> FindOrCreateLeagueFromBetExplorerAsync(
        LeagueMetadata providerLeague,
        Country country,
        string providerCode,
        Guid sportId)
    {
        try
        {
            _logger.LogInformation("FindOrCreate: Processing '{League}' from {Country} (provider: {Provider})",
                providerLeague.Name, country.Name, providerCode);

            // Step 1: Try to find existing league in configuration
            var existingLeague = await FindMatchingLeagueAsync(providerLeague, country, providerCode);
            if (existingLeague != null)
            {
                _logger.LogInformation("FindOrCreate: Found existing league '{League}' (ID: {Id})",
                    existingLeague.Name, existingLeague.Id);
                return existingLeague;
            }

            // Step 2: On-demand scrape BetExplorer for this country
            _logger.LogInformation("FindOrCreate: No existing league found, scraping BetExplorer for {Country}...",
                country.Name);

            var betExplorerLeagues = await GetBetExplorerLeaguesAsync(country);
            if (!betExplorerLeagues.Any())
            {
                _logger.LogWarning("FindOrCreate: No BetExplorer leagues found for {Country}", country.Name);
                return null;
            }

            // Step 3: Try to match the provider league to a BetExplorer league
            var providerCodeLower = providerCode.ToLowerInvariant();
            var countryCodeLower = country.Code.ToLowerInvariant();

            // First check manual mapping (with fallback to global rules)
            LeagueMetadata? betExplorerMatch = null;
            var mapping = await _mappingRepository.FindMappingWithFallbackAsync(
                providerCodeLower,
                countryCodeLower,
                providerLeague.Name);

            if (mapping != null)
            {
                betExplorerMatch = betExplorerLeagues.FirstOrDefault(l =>
                    l.Slug.Equals(mapping.BetExplorerSlug, StringComparison.OrdinalIgnoreCase));

                if (betExplorerMatch != null)
                {
                    _logger.LogInformation("FindOrCreate: Manual mapping found: '{ProviderLeague}' → '{BetExplorerLeague}'",
                        providerLeague.Name, betExplorerMatch.Name);
                }
            }

            // If no manual mapping, try automatic matching
            if (betExplorerMatch == null)
            {
                betExplorerMatch = FindBestMatch(providerLeague, betExplorerLeagues);
            }

            if (betExplorerMatch == null)
            {
                _logger.LogWarning("FindOrCreate: No BetExplorer match for '{League}' [{Country}]",
                    providerLeague.Name, country.Name);
                return null;
            }

            _logger.LogInformation("FindOrCreate: BetExplorer match found: '{ProviderLeague}' → '{BetExplorerLeague}' (slug: {Slug})",
                providerLeague.Name, betExplorerMatch.Name, betExplorerMatch.Slug);

            // Step 3.5: Validate league is round-based (not cup/knockout competition)
            var previousSeason = SeasonHelper.GetPreviousSeasonPattern("2024-2025");
            var isRoundBased = await _roundValidator.IsRoundBasedLeagueAsync(
                betExplorerMatch.Slug,
                country.Code.ToLowerInvariant(),
                previousSeason,
                Guid.Empty);  // providerId not needed for validation

            if (!isRoundBased)
            {
                _logger.LogInformation("FindOrCreate: Skipping cup/knockout competition '{League}' [{Country}] (detected by round structure)",
                    providerLeague.Name, country.Name);
                return null;  // Will be added to unmatched_leagues by caller
            }

            // Step 4: Check if league with this BetExplorer slug already exists
            var existingBySlug = await _leagueRepository.GetByBetExplorerSlugAsync(betExplorerMatch.Slug);
            if (existingBySlug != null)
            {
                _logger.LogInformation("FindOrCreate: League already exists with slug '{Slug}': '{League}'",
                    betExplorerMatch.Slug, existingBySlug.Name);
                return existingBySlug;
            }

            // Step 5: Create new league from BetExplorer data
            var newLeague = new League
            {
                SportId = sportId,
                CountryId = country.Id,
                Name = betExplorerMatch.Name,  // Use BetExplorer name as canonical
                DisplayName = $"{betExplorerMatch.Name} ({country.Name})",
                BetExplorerSlug = betExplorerMatch.Slug,
                IsBettable = true,  // From betting provider = bettable
                IsActive = true,
                Priority = betExplorerMatch.Priority,
                Notes = $"Created from {providerCode} via BetExplorer enrichment"
            };

            var createdLeague = await _leagueRepository.CreateAsync(newLeague);
            _logger.LogInformation("FindOrCreate: Created new league '{League}' (ID: {Id}, slug: {Slug})",
                createdLeague.Name, createdLeague.Id, createdLeague.BetExplorerSlug);

            // Step 6: Create LeagueProvider for BetExplorer (source of truth)
            await EnsureBetExplorerLeagueProviderAsync(createdLeague);

            return createdLeague;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FindOrCreate: Error processing '{League}' ({Country})",
                providerLeague.Name, country.Name);
            return null;
        }
    }

    /// <summary>
    /// Ensures a LeagueProvider mapping exists for BetExplorer provider.
    /// </summary>
    private async Task EnsureBetExplorerLeagueProviderAsync(League league)
    {
        try
        {
            // Find BetExplorer provider
            var providers = await _dataProviderRepository.GetAllAsync();
            var betExplorer = providers.FirstOrDefault(p =>
                p.Code.Equals("betexplorer", StringComparison.OrdinalIgnoreCase));

            if (betExplorer == null)
            {
                _logger.LogWarning("BetExplorer provider not found in configuration");
                return;
            }

            // Check if mapping already exists
            var existingMapping = await _leagueProviderRepository.GetByLeagueAndProviderAsync(
                league.Id, betExplorer.Id);

            if (existingMapping != null)
            {
                _logger.LogDebug("BetExplorer LeagueProvider mapping already exists for league {LeagueId}", league.Id);
                return;
            }

            // Create or update mapping
            var leagueProvider = new LeagueProvider
            {
                LeagueId = league.Id,
                ProviderId = betExplorer.Id,
                ProviderSlug = league.BetExplorerSlug,
                ProviderName = league.Name,
                IsActive = true
            };

            await _leagueProviderRepository.AddOrUpdateAsync(leagueProvider);
            _logger.LogInformation("Created/Updated BetExplorer LeagueProvider mapping for '{League}' (slug: {Slug})",
                league.Name, league.BetExplorerSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BetExplorer LeagueProvider for league {LeagueId}", league.Id);
        }
    }
}
