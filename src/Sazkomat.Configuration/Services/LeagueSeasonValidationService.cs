using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Models;
using Sazkomat.Configuration.Repositories;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Service for validating league seasons before locking.
/// </summary>
public class LeagueSeasonValidationService : ILeagueSeasonValidationService
{
    private readonly ILeagueSeasonRepository _leagueSeasonRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ILogger<LeagueSeasonValidationService> _logger;

    public LeagueSeasonValidationService(
        ILeagueSeasonRepository leagueSeasonRepository,
        ISeasonRepository seasonRepository,
        ILogger<LeagueSeasonValidationService> logger)
    {
        _leagueSeasonRepository = leagueSeasonRepository;
        _seasonRepository = seasonRepository;
        _logger = logger;
    }

    public async Task<LeagueSeasonValidationResult> ValidateAsync(Guid leagueSeasonId)
    {
        var result = new LeagueSeasonValidationResult();

        var leagueSeason = await _leagueSeasonRepository.GetByIdAsync(leagueSeasonId);
        if (leagueSeason == null)
        {
            result.Issues.Add(new ValidationIssue
            {
                Code = "NOT_FOUND",
                Message = "League season not found",
                Severity = IssueSeverity.Error
            });
            return result;
        }

        _logger.LogInformation(
            "Validating league season {LeagueSeasonId} ({League} - {Season})",
            leagueSeasonId, leagueSeason.League?.DisplayName, leagueSeason.Season?.Name);

        // Ignored seasons return clean result (no issues)
        if (leagueSeason.IsIgnored)
        {
            _logger.LogInformation(
                "Season {LeagueSeasonId} is ignored, skipping validation",
                leagueSeasonId);
            return result;
        }

        // Rule 1: Historical season without data
        if (leagueSeason.SyncMode == SyncMode.Historical && !leagueSeason.HasData)
        {
            if (leagueSeason.LastDataSyncAt == null)
            {
                // Sync neproběhl vůbec → Error (blokující)
                result.Issues.Add(new ValidationIssue
                {
                    Code = "NOT_SYNCED",
                    Message = "Historická sezóna nebyla synchronizována",
                    Severity = IssueSeverity.Error
                });
            }
            else
            {
                // Sync proběhl, ale prázdný výsledek → Warning (lze zamknout)
                result.Issues.Add(new ValidationIssue
                {
                    Code = "NO_DATA_AFTER_SYNC",
                    Message = "Sezóna byla zpracována, ale nemá žádná data (může být legitimní pro staré sezóny)",
                    Severity = IssueSeverity.Warning
                });
            }
        }

        // Rule 2: Parsing error
        if (leagueSeason.NoDataReason == NoDataReason.ParsingError)
        {
            result.Issues.Add(new ValidationIssue
            {
                Code = "PARSING_ERROR",
                Message = $"Chyba parsování: {leagueSeason.NoDataNote ?? "neznámá chyba"}",
                Severity = IssueSeverity.Error
            });
        }

        // Rule 3: Network error
        if (leagueSeason.NoDataReason == NoDataReason.NetworkError)
        {
            result.Issues.Add(new ValidationIssue
            {
                Code = "NETWORK_ERROR",
                Message = $"Síťová chyba: {leagueSeason.NoDataNote ?? "neznámá chyba"}",
                Severity = IssueSeverity.Error
            });
        }

        // Rule 4: Has data but no successful recipe (warning)
        if (leagueSeason.HasData && leagueSeason.LastSuccessfulRecipeId == null)
        {
            result.Issues.Add(new ValidationIssue
            {
                Code = "NO_RECIPE",
                Message = "Data existují, ale není zaznamenán úspěšný recept",
                Severity = IssueSeverity.Warning
            });
        }

        // Rule 5 & 6: Unusual matches/rounds count compared to league average
        if (leagueSeason.HasData)
        {
            await CheckUnusualCountsAsync(leagueSeason, result);
        }

        // Rule 7: Page not found (warning - can be legitimate)
        if (leagueSeason.NoDataReason == NoDataReason.PageNotFound)
        {
            result.Issues.Add(new ValidationIssue
            {
                Code = "PAGE_NOT_FOUND",
                Message = "Stránka neexistuje na BetExploreru (může být legitimní pro staré sezóny)",
                Severity = IssueSeverity.Warning
            });
        }

        _logger.LogInformation(
            "Validation completed for {LeagueSeasonId}: {IssueCount} issues, CanBeLocked={CanBeLocked}",
            leagueSeasonId, result.Issues.Count, result.CanBeLocked);

        return result;
    }

    private async Task CheckUnusualCountsAsync(LeagueSeason leagueSeason, LeagueSeasonValidationResult result)
    {
        // Get all seasons for this league to calculate average
        var allSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(leagueSeason.LeagueId);
        var seasonsWithData = allSeasons.Where(s => s.HasData && s.Id != leagueSeason.Id).ToList();

        if (seasonsWithData.Count < 2)
        {
            // Not enough data to compare
            return;
        }

        var avgMatches = seasonsWithData.Average(s => s.MatchesCount);
        var avgRounds = seasonsWithData.Average(s => s.RoundsCount);

        // Check if current season is outside 50-200% of average
        if (avgMatches > 0)
        {
            var matchRatio = leagueSeason.MatchesCount / avgMatches;
            if (matchRatio < 0.5 || matchRatio > 2.0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "UNUSUAL_MATCHES_COUNT",
                    Message = $"Neobvyklý počet zápasů: {leagueSeason.MatchesCount} (průměr ligy: {avgMatches:F0})",
                    Severity = IssueSeverity.Warning
                });
            }
        }

        if (avgRounds > 0)
        {
            var roundRatio = leagueSeason.RoundsCount / avgRounds;
            if (roundRatio < 0.5 || roundRatio > 2.0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "UNUSUAL_ROUNDS_COUNT",
                    Message = $"Neobvyklý počet kol: {leagueSeason.RoundsCount} (průměr ligy: {avgRounds:F0})",
                    Severity = IssueSeverity.Warning
                });
            }
        }
    }

    public async Task<LeagueValidationResult> ValidateLeagueAsync(Guid leagueId)
    {
        _logger.LogInformation("Validating all historical seasons for league {LeagueId}", leagueId);

        var result = new LeagueValidationResult();
        var allSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(leagueId, includeRelations: true);
        var currentYear = DateTime.UtcNow.Year;

        // Filter to only historical seasons (not Current, not Future, not already locked, not ignored)
        var historicalSeasons = allSeasons
            .Where(s => s.SyncMode == SyncMode.Historical)
            .Where(s => !s.IsIgnored)
            .Where(s => s.Season != null && s.Season.StartYear <= currentYear)  // Not future seasons
            .ToList();

        var alreadyLocked = historicalSeasons.Where(s => s.IsLocked).ToList();
        var toValidate = historicalSeasons.Where(s => !s.IsLocked).ToList();

        result.AlreadyLockedCount = alreadyLocked.Count;
        result.TotalSeasons = toValidate.Count;

        foreach (var leagueSeason in toValidate)
        {
            var seasonValidation = await ValidateAsync(leagueSeason.Id);
            var seasonName = leagueSeason.Season?.Name ?? "Unknown";

            var seasonResult = new SeasonValidationResultItem
            {
                SeasonId = leagueSeason.Id,
                SeasonName = seasonName,
                IsValid = seasonValidation.IsValid,
                CanBeLocked = seasonValidation.CanBeLocked,
                Issues = seasonValidation.Issues
            };

            result.SeasonResults.Add(seasonResult);

            if (seasonValidation.IsValid)
            {
                result.ValidSeasons++;
                result.CanLockCount++;
            }
            else if (seasonValidation.CanBeLocked)
            {
                result.SeasonsWithWarnings++;
                result.CanLockCount++;
            }
            else
            {
                result.SeasonsWithErrors++;
            }
        }

        _logger.LogInformation(
            "League {LeagueId} validation completed: {Total} seasons, {Valid} valid, {Warnings} with warnings, {Errors} with errors, {CanLock} can be locked, {AlreadyLocked} already locked",
            leagueId, result.TotalSeasons, result.ValidSeasons, result.SeasonsWithWarnings, result.SeasonsWithErrors, result.CanLockCount, result.AlreadyLockedCount);

        return result;
    }

    public async Task<int> LockValidSeasonsAsync(Guid leagueId)
    {
        _logger.LogInformation("Locking valid historical seasons for league {LeagueId}", leagueId);

        var allSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(leagueId, includeRelations: true);
        var currentYear = DateTime.UtcNow.Year;

        // Filter to only historical, unlocked, non-ignored seasons
        var toValidateAndLock = allSeasons
            .Where(s => s.SyncMode == SyncMode.Historical)
            .Where(s => !s.IsLocked)
            .Where(s => !s.IsIgnored)
            .Where(s => s.Season != null && s.Season.StartYear <= currentYear)  // Not future seasons
            .ToList();

        int lockedCount = 0;

        foreach (var leagueSeason in toValidateAndLock)
        {
            var validation = await ValidateAsync(leagueSeason.Id);

            if (validation.CanBeLocked)
            {
                await _leagueSeasonRepository.UpdateLockStatusAsync(leagueSeason.Id, true);
                await _leagueSeasonRepository.UpdateLastValidatedAsync(leagueSeason.Id);
                lockedCount++;

                _logger.LogInformation(
                    "Locked season {SeasonName} for league {LeagueId} (warnings: {WarningCount})",
                    leagueSeason.Season?.Name, leagueId, validation.Issues.Count(i => i.Severity == IssueSeverity.Warning));
            }
        }

        _logger.LogInformation("Locked {Count} seasons for league {LeagueId}", lockedCount, leagueId);
        return lockedCount;
    }

    public async Task<int> UnlockAllSeasonsAsync(Guid leagueId)
    {
        _logger.LogInformation("Unlocking all seasons for league {LeagueId}", leagueId);

        var allSeasons = await _leagueSeasonRepository.GetByLeagueIdAsync(leagueId);
        var lockedSeasons = allSeasons.Where(s => s.IsLocked).ToList();

        foreach (var leagueSeason in lockedSeasons)
        {
            await _leagueSeasonRepository.UpdateLockStatusAsync(leagueSeason.Id, false);
        }

        _logger.LogInformation("Unlocked {Count} seasons for league {LeagueId}", lockedSeasons.Count, leagueId);
        return lockedSeasons.Count;
    }
}
