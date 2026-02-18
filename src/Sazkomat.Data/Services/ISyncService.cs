using Sazkomat.Core.Common;
using Sazkomat.Data.DTOs;

namespace Sazkomat.Data.Services;

public interface ISyncService
{
    /// <summary>
    /// Synchronizes countries from the provider for all active sports
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="activateCountries">If true, automatically activates matched countries that are currently inactive</param>
    Task<Result<SyncResponse>> SyncCountriesAsync(Guid providerId, bool activateCountries = false);

    /// <summary>
    /// Synchronizes leagues from the provider for active countries
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="countryId">Optional: specific country to sync</param>
    Task<Result<SyncResponse>> SyncLeaguesAsync(Guid providerId, Guid? countryId = null);

    /// <summary>
    /// Synchronizes seasons for a specific league
    /// </summary>
    Task<Result<SyncResponse>> SyncSeasonsAsync(Guid providerId, Guid leagueId);

    /// <summary>
    /// Synchronizes seasons for all active leagues (limited to last 3 years)
    /// </summary>
    Task<Result<SyncResponse>> SyncAllActiveSeasonsAsync(Guid providerId);

    /// <summary>
    /// Scans ALL available seasons from BetExplorer for leagues with betting provider mapping.
    /// Unlike SyncAllActiveSeasonsAsync, this has no year limit - shows all historical seasons.
    /// </summary>
    /// <param name="leagueIds">Optional: specific leagues to scan. If null, scans all leagues with betting provider mapping.</param>
    Task<Result<SyncResponse>> GlobalSeasonScanAsync(List<Guid>? leagueIds = null);

    /// <summary>
    /// Gets the current sync status
    /// </summary>
    Task<SyncStatusResponse> GetSyncStatusAsync();

    /// <summary>
    /// Resets sync status (clears IsRunning lock) - use when sync is stuck
    /// </summary>
    void ResetSyncStatus();

    /// <summary>
    /// Synchronizes season data (rounds, matches) for all seasons of a specific league.
    /// Historical seasons with HasData=true are automatically skipped unless forceUpdate is true.
    /// Fail-fast: stops on first error.
    /// </summary>
    /// <param name="leagueId">League ID to sync seasons for</param>
    /// <param name="forceUpdate">If true, re-sync seasons even if they already have data</param>
    /// <returns>SyncJob ID for tracking progress</returns>
    Task<Result<Guid>> SyncLeagueSeasonDataAsync(Guid leagueId, bool forceUpdate = false);

    /// <summary>
    /// Refreshes the list of available seasons for a league from BetExplorer.
    /// Only creates LeagueSeason entries, does not sync rounds/matches.
    /// </summary>
    /// <param name="leagueId">League ID to refresh seasons for</param>
    /// <returns>SyncJob ID for tracking progress</returns>
    Task<Result<Guid>> RefreshLeagueSeasonsListAsync(Guid leagueId);
}
