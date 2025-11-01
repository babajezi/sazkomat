using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

/// <summary>
/// Tracks the state of the synchronization workflow.
/// This is a singleton entity - only one record should exist in the database.
/// </summary>
public class SyncWorkflowState : Entity
{
    /// <summary>
    /// Countries have been synchronized from the provider
    /// </summary>
    public bool CountriesSynced { get; set; }

    /// <summary>
    /// User has confirmed country selection (marked active countries)
    /// </summary>
    public bool CountriesConfirmed { get; set; }

    /// <summary>
    /// Leagues have been synchronized from the provider (only for active countries)
    /// </summary>
    public bool LeaguesSynced { get; set; }

    /// <summary>
    /// User has confirmed league selection (marked active/bettable leagues)
    /// </summary>
    public bool LeaguesConfirmed { get; set; }

    /// <summary>
    /// Seasons have been synchronized from the provider (only for active leagues)
    /// </summary>
    public bool SeasonsSynced { get; set; }

    /// <summary>
    /// Timestamp when countries were synced
    /// </summary>
    public DateTime? CountriesSyncedAt { get; set; }

    /// <summary>
    /// Timestamp when leagues were synced
    /// </summary>
    public DateTime? LeaguesSyncedAt { get; set; }

    /// <summary>
    /// Timestamp when seasons were synced
    /// </summary>
    public DateTime? SeasonsSyncedAt { get; set; }

    /// <summary>
    /// Resets the entire workflow state
    /// </summary>
    public void Reset()
    {
        CountriesSynced = false;
        CountriesConfirmed = false;
        LeaguesSynced = false;
        LeaguesConfirmed = false;
        SeasonsSynced = false;
        CountriesSyncedAt = null;
        LeaguesSyncedAt = null;
        SeasonsSyncedAt = null;
    }
}
