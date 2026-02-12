using System.ComponentModel.DataAnnotations.Schema;
using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class League : Entity
{
    public Guid SportId { get; set; }
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameCs { get; set; } // Czech name for localization
    public string DisplayName { get; set; } = string.Empty;
    [Obsolete("Use LeagueProvider mapping instead")]
    public string BetExplorerSlug { get; set; } = string.Empty; // Will be removed in next migration
    public bool IsBettable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 5;
    public string? Notes { get; set; }

    // Aggregated season stats (not mapped to database)
    [NotMapped]
    public int HistoricalSeasonsCount { get; set; }

    [NotMapped]
    public int LockedSeasonsCount { get; set; }

    [NotMapped]
    public int SeasonsWithDataCount { get; set; }

    // Navigation
    public Sport Sport { get; set; } = null!;
    public Country Country { get; set; } = null!;
    public ICollection<LeagueSeason> LeagueSeasons { get; set; } = new List<LeagueSeason>();
    public ICollection<LeagueProvider> LeagueProviders { get; set; } = new List<LeagueProvider>();
}
