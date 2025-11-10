using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public interface IMatchRepository
{
    Task<List<Match>> GetAllAsync(MatchFilter? filter = null);
    Task<Match?> GetByIdAsync(Guid id);
    Task<List<Match>> GetByRoundIdAsync(Guid roundId);
    Task<int> GetCountAsync(MatchFilter? filter = null);
    Task<Match> CreateAsync(Match match);
    Task<Match> UpdateAsync(Match match);
    Task DeleteAsync(Guid id);
}

public class MatchFilter
{
    public Guid? LeagueId { get; set; }
    public Guid? SeasonId { get; set; }
    public int? RoundNumber { get; set; }
    public string? Result { get; set; } // H, D, A
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? TeamName { get; set; } // Search in both HomeTeam and AwayTeam
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public string? SortBy { get; set; } // "date", "league", "round"
    public bool SortDescending { get; set; } = false;
}
