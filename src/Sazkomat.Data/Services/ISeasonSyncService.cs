using Sazkomat.Core.Common;
using Sazkomat.Data.DTOs;

namespace Sazkomat.Data.Services;

/// <summary>
/// Service for synchronizing season data (rounds and matches) from providers
/// </summary>
public interface ISeasonSyncService
{
    /// <summary>
    /// Syncs data (rounds and matches) for a single season
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="leagueId">League ID</param>
    /// <param name="seasonId">Season ID</param>
    /// <param name="forceUpdate">Force update even if data already exists (for historical seasons)</param>
    /// <returns>Sync response with statistics</returns>
    Task<Result<SyncResponse>> SyncSeasonDataAsync(
        Guid providerId,
        Guid leagueId,
        Guid seasonId,
        bool forceUpdate = false);

    /// <summary>
    /// Syncs data for all league seasons where SyncEnabled = true
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>Aggregated sync response</returns>
    Task<Result<SyncResponse>> SyncAllMarkedSeasonsDataAsync(Guid providerId);

    /// <summary>
    /// Detects and marks current seasons based on provider's CurrentSeasonPatterns
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DetectAndMarkCurrentSeasonsAsync(Guid providerId);
}
