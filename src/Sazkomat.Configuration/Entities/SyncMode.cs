namespace Sazkomat.Configuration.Entities;

/// <summary>
/// Defines the synchronization mode for league seasons
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Historical mode - data is synced once and not updated afterwards
    /// Used for closed/finished seasons
    /// </summary>
    Historical,

    /// <summary>
    /// Current mode - data is synced continuously to capture new rounds/matches
    /// Used for ongoing seasons
    /// </summary>
    Current,

    /// <summary>
    /// Future mode - season has not started yet, no sync needed
    /// Used for seasons with StartYear > current year
    /// </summary>
    Future
}
