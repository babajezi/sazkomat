using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Scrapers;

/// <summary>
/// Result of a scraping operation with success/failure information
/// </summary>
public class ScrapeResult
{
    public List<Round> Rounds { get; }
    public NoDataReason? FailureReason { get; }
    public string? ErrorMessage { get; }

    /// <summary>
    /// Number of round headers found on the page (may differ from Rounds.Count if matches are cancelled)
    /// </summary>
    public int TotalRoundHeadersFound { get; }

    /// <summary>
    /// Number of match rows found on the page (even if not assigned to rounds)
    /// </summary>
    public int TotalMatchRowsFound { get; }

    public bool IsSuccess => FailureReason == null || FailureReason == NoDataReason.None;

    /// <summary>
    /// True if some rounds were found on page but have no match data (e.g., cancelled season)
    /// </summary>
    public bool IsPartialData => TotalRoundHeadersFound > 0 && Rounds.Count < TotalRoundHeadersFound;

    /// <summary>
    /// True if page has match results but no round structure
    /// </summary>
    public bool HasResultsWithoutRounds => TotalMatchRowsFound > 0 && Rounds.Count == 0;

    private ScrapeResult(List<Round> rounds, NoDataReason? failureReason, string? errorMessage, int totalRoundHeadersFound = 0, int totalMatchRowsFound = 0)
    {
        Rounds = rounds;
        FailureReason = failureReason;
        ErrorMessage = errorMessage;
        TotalRoundHeadersFound = totalRoundHeadersFound > 0 ? totalRoundHeadersFound : rounds.Count;
        TotalMatchRowsFound = totalMatchRowsFound > 0 ? totalMatchRowsFound : rounds.Sum(r => r.MatchesCount);
    }

    /// <summary>
    /// Create success result. If rounds is empty, sets NoRoundsFound or NoResults reason.
    /// </summary>
    public static ScrapeResult Success(List<Round> rounds, int totalRoundHeadersFound = 0, int totalMatchRowsFound = 0)
    {
        if (rounds.Count > 0)
        {
            return new(rounds, null, null, totalRoundHeadersFound, totalMatchRowsFound);
        }

        // No rounds found - distinguish between "has results" and "empty page"
        var reason = totalMatchRowsFound > 0 ? NoDataReason.NoRoundsFound : NoDataReason.NoResults;
        return new(rounds, reason, null, totalRoundHeadersFound, totalMatchRowsFound);
    }

    /// <summary>
    /// Create result for page not found (301/404 redirect)
    /// </summary>
    public static ScrapeResult PageNotFound(string url)
        => new(new List<Round>(), NoDataReason.PageNotFound, $"Page not found: {url}");

    /// <summary>
    /// Create result for HTML parsing error
    /// </summary>
    public static ScrapeResult ParsingError(string message)
        => new(new List<Round>(), NoDataReason.ParsingError, message);

    /// <summary>
    /// Create result for network/timeout error
    /// </summary>
    public static ScrapeResult NetworkError(string message)
        => new(new List<Round>(), NoDataReason.NetworkError, message);
}
