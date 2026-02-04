using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class LeagueSeason : Entity
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public bool IsAvailableOnBetExplorer { get; set; } = true;
    public bool HasData { get; set; } = false;
    public NoDataReason? NoDataReason { get; set; }
    public string? NoDataNote { get; set; }
    public bool HasOdds { get; set; } = false;
    public DateTime? LastScrapedAt { get; set; }
    public int RoundsCount { get; set; } = 0;
    public int MatchesCount { get; set; } = 0;

    // Sync flags
    public bool SyncEnabled { get; set; } = false;
    public bool IsCurrent { get; set; } = false;
    public SyncMode SyncMode { get; set; } = SyncMode.Historical;
    public DateTime? LastDataSyncAt { get; set; }

    // Recipe tracking for adaptive scraping
    /// <summary>
    /// ID of the last recipe that successfully scraped this season.
    /// Used to prioritize this recipe on subsequent syncs.
    /// </summary>
    public Guid? LastSuccessfulRecipeId { get; set; }

    /// <summary>
    /// When recipes were last tested for this season.
    /// If LastSuccessfulRecipeId is null after testing, no recipe worked.
    /// </summary>
    public DateTime? LastRecipeTestedAt { get; set; }

    // Validation and locking
    /// <summary>
    /// Whether this season is locked (validated and finalized).
    /// Locked seasons cannot be synced or modified.
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// When the season was locked.
    /// </summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// When the season was last validated.
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    // Navigation
    public League League { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
