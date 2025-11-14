using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for live synchronization of current rounds and matches.
/// LiveSync = Direct import of current data without caching (for active seasons).
/// </summary>
public interface ILiveSyncService
{
    /// <summary>
    /// Synchronizes current rounds and matches for specified leagues.
    /// Creates a SyncJob with type=LiveUpdate and entity_type=Rounds.
    /// Directly scrapes and imports current round data without caching.
    /// </summary>
    /// <param name="providerId">The data provider ID</param>
    /// <param name="leagueIds">List of league IDs to sync. If null/empty, syncs all enabled leagues.</param>
    /// <param name="forceRefresh">If true, re-scrapes even if data exists for current round</param>
    /// <returns>SyncJob ID</returns>
    Task<Guid> LiveSyncRoundsAsync(Guid providerId, List<Guid>? leagueIds = null, bool forceRefresh = false);

    /// <summary>
    /// Synchronizes a specific round by round ID.
    /// Used for updating an existing round with latest match results.
    /// </summary>
    /// <param name="providerId">The data provider ID</param>
    /// <param name="roundId">The round ID to sync</param>
    /// <returns>SyncJob ID</returns>
    Task<Guid> LiveSyncRoundAsync(Guid providerId, Guid roundId);

    /// <summary>
    /// Internal method: Synchronizes rounds using an existing job.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task LiveSyncRoundsInternalAsync(Guid jobId, Guid providerId, List<Guid>? leagueIds = null, bool forceRefresh = false);

    /// <summary>
    /// Internal method: Synchronizes a specific round using an existing job.
    /// Called by SyncJobProcessor to avoid duplicate job creation.
    /// </summary>
    Task LiveSyncRoundInternalAsync(Guid jobId, Guid providerId, Guid roundId);

    /// <summary>
    /// Gets statistics about rounds that need live sync.
    /// Returns counts of leagues with active seasons and rounds needing updates.
    /// </summary>
    Task<LiveSyncStats> GetLiveSyncStatsAsync(Guid providerId);
}

public record LiveSyncStats(
    int ActiveLeagues,
    int TotalRounds,
    int RoundsNeedingUpdate,
    DateTime? LastSyncAt
);
