namespace Sazkomat.Data.DTOs;

public class SyncRequest
{
    public Guid ProviderId { get; set; }
    public SyncType Type { get; set; }
    public Guid? EntityId { get; set; } // LeagueId for season sync, CountryId for league sync, etc.
    public bool ActivateCountries { get; set; } = false; // Auto-activate matched countries during sync
}

public enum SyncType
{
    Countries,
    Leagues,
    Seasons
}
