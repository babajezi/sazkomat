using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

public class Match : Entity
{
    public Guid RoundId { get; set; }
    public Guid ProviderId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Result { get; set; } = string.Empty; // H/D/A
    public decimal? HomeOdds { get; set; }
    public decimal? DrawOdds { get; set; }
    public decimal? AwayOdds { get; set; }
    public DateTime? MatchDate { get; set; }
    [Obsolete("Use ProviderUrl instead")]
    public string? BetExplorerUrl { get; set; }
    public string? ProviderUrl { get; set; }

    // Navigation
    public Round Round { get; set; } = null!;
}
