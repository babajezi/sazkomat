using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.Data.Entities;

public class ImportJob : Entity
{
    public Guid LeagueId { get; set; }
    public Guid ProviderId { get; set; }
    public ImportJobType Type { get; set; }
    public ImportJobStatus Status { get; set; }
    public List<Guid> SeasonIds { get; set; } = new();
    public bool IncludeWithoutOdds { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ImportProgressData Progress { get; set; } = new();

    // Navigation
    public League League { get; set; } = null!;
}
