using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

public class Round : Entity
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid ProviderId { get; set; }
    public int RoundNumber { get; set; }
    public string? GroupName { get; set; }  // null = liga bez skupin, e.g. "East", "West", "GROUP 1"
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MatchesCount { get; set; }
    public int HomeWins { get; set; }
    public int Draws { get; set; }
    public int AwayWins { get; set; }
    public decimal CumulativeOddsHome { get; set; } = 1.0m;
    public decimal CumulativeOddsDraw { get; set; } = 1.0m;
    public decimal CumulativeOddsAway { get; set; } = 1.0m;
    public string SummaryResult { get; set; } = string.Empty;
    public string OddsComplete { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; }
    [Obsolete("Use ProviderId instead")]
    public string DataSource { get; set; } = "betexplorer.com";

    // Navigation
    public League League { get; set; } = null!;
    public Season Season { get; set; } = null!;
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
