using Sazkomat.Configuration.Models;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Service for validating league seasons before locking.
/// </summary>
public interface ILeagueSeasonValidationService
{
    /// <summary>
    /// Validates a single league season and returns any issues found.
    /// </summary>
    /// <param name="leagueSeasonId">The ID of the league season to validate.</param>
    /// <returns>Validation result with list of issues.</returns>
    Task<LeagueSeasonValidationResult> ValidateAsync(Guid leagueSeasonId);

    /// <summary>
    /// Validates all historical seasons for a league.
    /// Only validates Historical mode seasons that are not locked.
    /// </summary>
    /// <param name="leagueId">The ID of the league to validate.</param>
    /// <returns>League-level validation result with summary and per-season details.</returns>
    Task<LeagueValidationResult> ValidateLeagueAsync(Guid leagueId);

    /// <summary>
    /// Locks all valid historical seasons for a league (seasons with no Error-level issues).
    /// </summary>
    /// <param name="leagueId">The ID of the league.</param>
    /// <returns>Number of seasons that were locked.</returns>
    Task<int> LockValidSeasonsAsync(Guid leagueId);

    /// <summary>
    /// Unlocks all locked seasons for a league.
    /// </summary>
    /// <param name="leagueId">The ID of the league.</param>
    /// <returns>Number of seasons that were unlocked.</returns>
    Task<int> UnlockAllSeasonsAsync(Guid leagueId);
}
