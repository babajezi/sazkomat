using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Scrapers;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Orchestrates betting provider scrapers and manages league availability sync
/// </summary>
public class BettingProviderOrchestrator
{
    private readonly IEnumerable<IBettingProviderScraper> _scrapers;
    private readonly ILeagueProviderRepository _leagueProviderRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly IDataProviderRepository _dataProviderRepository;
    private readonly SyncQueueService _syncQueueService;
    private readonly ILogger<BettingProviderOrchestrator> _logger;

    public BettingProviderOrchestrator(
        IEnumerable<IBettingProviderScraper> scrapers,
        ILeagueProviderRepository leagueProviderRepository,
        ILeagueRepository leagueRepository,
        IDataProviderRepository dataProviderRepository,
        SyncQueueService syncQueueService,
        ILogger<BettingProviderOrchestrator> logger)
    {
        _scrapers = scrapers;
        _leagueProviderRepository = leagueProviderRepository;
        _leagueRepository = leagueRepository;
        _dataProviderRepository = dataProviderRepository;
        _syncQueueService = syncQueueService;
        _logger = logger;
    }

    /// <summary>
    /// Syncs available leagues from a specific betting provider
    /// </summary>
    public async Task<Result> SyncLeagueAvailabilityAsync(string providerCode, string sportCode)
    {
        // Try to acquire sync lock
        if (!await _syncQueueService.TryAcquireLockAsync(providerCode, sportCode))
        {
            return Result.Failure("Synchronization is already running for this provider and sport");
        }

        try
        {
            _logger.LogInformation("Starting league availability sync for provider: {ProviderCode}, sport: {SportCode}",
                providerCode, sportCode);

            // Find the scraper (case-insensitive)
            var scraper = _scrapers.FirstOrDefault(s =>
                s.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));
            if (scraper == null)
            {
                return Result.Failure($"No scraper found for provider: {providerCode}");
            }

            // Get the provider from database
            var provider = await _dataProviderRepository.GetByCodeAsync(providerCode);
            if (provider == null)
            {
                return Result.Failure($"Provider '{providerCode}' not found in database");
            }

            // Scrape available leagues
            var leaguesResult = await scraper.GetAvailableLeaguesAsync(sportCode);
            if (!leaguesResult.IsSuccess)
            {
                return Result.Failure($"Failed to scrape leagues: {leaguesResult.Error}");
            }

            var scrapedLeagues = leaguesResult.Value!;
            _logger.LogInformation("Scraped {Count} leagues from {Provider}", scrapedLeagues.Count, providerCode);

            // Create/update LeagueProvider mappings
            int created = 0;
            int updated = 0;

            foreach (var scrapedLeague in scrapedLeagues)
            {
                // Try to find matching league in database (by name similarity)
                var matchingLeague = await TryFindMatchingLeagueAsync(scrapedLeague);

                if (matchingLeague != null)
                {
                    // Check if mapping already exists
                    var existingMapping = await _leagueProviderRepository
                        .GetByLeagueAndProviderAsync(matchingLeague.Id, provider.Id);

                    if (existingMapping == null)
                    {
                        // Create new mapping
                        var newMapping = new LeagueProvider
                        {
                            LeagueId = matchingLeague.Id,
                            ProviderId = provider.Id,
                            ProviderSlug = scrapedLeague.ProviderLeagueId,
                            ProviderName = scrapedLeague.ProviderLeagueName,
                            IsActive = false, // Don't activate automatically
                            Metadata = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                Url = scrapedLeague.ProviderUrl,
                                CountryCode = scrapedLeague.CountryCode,
                                CountryName = scrapedLeague.CountryName
                            })
                        };

                        await _leagueProviderRepository.AddOrUpdateAsync(newMapping);
                        created++;
                        _logger.LogInformation("Created/Updated mapping: {LeagueName} -> {ProviderName}",
                            matchingLeague.Name, scrapedLeague.ProviderLeagueName);
                    }
                    else
                    {
                        // Update existing mapping
                        existingMapping.ProviderName = scrapedLeague.ProviderLeagueName;
                        existingMapping.Metadata = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Url = scrapedLeague.ProviderUrl,
                            CountryCode = scrapedLeague.CountryCode,
                            CountryName = scrapedLeague.CountryName
                        });

                        await _leagueProviderRepository.UpdateAsync(existingMapping);
                        updated++;
                    }
                }
                else
                {
                    _logger.LogWarning("Could not match scraped league: {LeagueName} ({Provider})",
                        scrapedLeague.ProviderLeagueName, providerCode);
                }
            }

            _logger.LogInformation("Sync completed: {Created} created, {Updated} updated", created, updated);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing league availability for {Provider}", providerCode);
            return Result.Failure($"Sync failed: {ex.Message}");
        }
        finally
        {
            // Always release the lock
            await _syncQueueService.ReleaseLockAsync(providerCode, sportCode);
        }
    }

    /// <summary>
    /// Get current sync status for a provider
    /// </summary>
    public async Task<string> GetSyncStatusAsync(string providerCode, string? sportCode = null)
    {
        return await _syncQueueService.GetSyncStatusAsync(providerCode, sportCode);
    }

    /// <summary>
    /// Automatically enables BetExplorer sync for leagues that have betting support
    /// </summary>
    public async Task<Result> AutoEnableBetExplorerSyncAsync()
    {
        try
        {
            _logger.LogInformation("Starting auto-enable BetExplorer sync for leagues with betting support");

            // Get BetExplorer provider
            var betExplorerProvider = await _dataProviderRepository.GetByCodeAsync("betexplorer");
            if (betExplorerProvider == null)
            {
                return Result.Failure("BetExplorer provider not found");
            }

            // Get all leagues
            var allLeagues = await _leagueRepository.GetAllAsync();
            int enabled = 0;

            foreach (var league in allLeagues)
            {
                // Check if league has any betting provider mappings
                var bettingProviderMappings = await _leagueProviderRepository
                    .GetByLeagueIdAsync(league.Id);

                var hasBettingSupport = bettingProviderMappings.Any(m =>
                    m.Provider.Type == ProviderType.BettingProvider);

                if (hasBettingSupport)
                {
                    // Enable BetExplorer sync for this league
                    var betExplorerMapping = bettingProviderMappings
                        .FirstOrDefault(m => m.ProviderId == betExplorerProvider.Id);

                    if (betExplorerMapping != null && !betExplorerMapping.IsActive)
                    {
                        betExplorerMapping.IsActive = true;
                        await _leagueProviderRepository.UpdateAsync(betExplorerMapping);
                        enabled++;
                        _logger.LogInformation("Enabled BetExplorer sync for league: {LeagueName}", league.Name);
                    }
                }
            }

            _logger.LogInformation("Auto-enable completed: {Count} leagues enabled", enabled);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-enabling BetExplorer sync");
            return Result.Failure($"Auto-enable failed: {ex.Message}");
        }
    }

    private async Task<League?> TryFindMatchingLeagueAsync(Entities.LeagueAvailability scrapedLeague)
    {
        // Simple name-based matching for now
        // TODO: Implement fuzzy matching (Levenshtein distance) for better accuracy

        var allLeagues = await _leagueRepository.GetAllAsync();

        // Exact match
        var exactMatch = allLeagues.FirstOrDefault(l =>
            l.Name.Equals(scrapedLeague.ProviderLeagueName, StringComparison.OrdinalIgnoreCase) ||
            l.DisplayName.Equals(scrapedLeague.ProviderLeagueName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
            return exactMatch;

        // Contains match
        var containsMatch = allLeagues.FirstOrDefault(l =>
            l.Name.Contains(scrapedLeague.ProviderLeagueName, StringComparison.OrdinalIgnoreCase) ||
            scrapedLeague.ProviderLeagueName.Contains(l.Name, StringComparison.OrdinalIgnoreCase));

        return containsMatch;
    }
}
