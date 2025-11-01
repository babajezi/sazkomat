using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class Season : Entity
{
    public string Name { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public int? EndYear { get; set; }

    // Navigation
    public ICollection<LeagueSeason> LeagueSeasons { get; set; } = new List<LeagueSeason>();
}
