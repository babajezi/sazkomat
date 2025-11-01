using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class LeagueProvider : Entity
{
    public Guid LeagueId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderSlug { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ProviderLeagueId { get; set; } // Some providers use numeric IDs
    public string? Metadata { get; set; } // JSONB for additional provider-specific data

    // Navigation properties
    public League League { get; set; } = null!;
    public DataProvider Provider { get; set; } = null!;
}
