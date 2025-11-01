using Sazkomat.Core.Common;
using Sazkomat.DataImport.DTOs;

namespace Sazkomat.DataImport.Services;

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
    /// Gets the current sync status
    /// </summary>
    Task<SyncStatusResponse> GetSyncStatusAsync();

    /// <summary>
    /// Resets sync status (clears IsRunning lock) - use when sync is stuck
    /// </summary>
    void ResetSyncStatus();
}
