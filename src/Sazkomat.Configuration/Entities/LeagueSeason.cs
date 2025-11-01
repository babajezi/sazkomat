using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class LeagueSeason : Entity
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public bool IsAvailableOnBetExplorer { get; set; } = true;
    public bool HasData { get; set; } = false;
    public bool HasOdds { get; set; } = false;
    public DateTime? LastScrapedAt { get; set; }
    public int RoundsCount { get; set; } = 0;
    public int MatchesCount { get; set; } = 0;

    // Sync flags
    public bool SyncEnabled { get; set; } = false;
    public bool IsCurrent { get; set; } = false;
    public SyncMode SyncMode { get; set; } = SyncMode.Historical;
    public DateTime? LastDataSyncAt { get; set; }

    // Navigation
    public League League { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
