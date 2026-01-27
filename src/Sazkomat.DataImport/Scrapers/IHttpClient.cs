namespace Sazkomat.DataImport.Scrapers;

public interface IHttpClient
{
    Task<string> GetHtmlAsync(string url);

    /// <summary>
    /// Fetches HTML from BetExplorer results page for a specific season.
    /// Uses JavaScript interaction to select season, navigate to Results tab, and sort by round.
    /// </summary>
    /// <param name="baseLeagueUrl">Base league URL like /football/hungary/nb-i/</param>
    /// <param name="season">Season in format "2020-2021" or "2020/2021" (null for current)</param>
    /// <param name="debugSavePath">Optional path to save HTML for debugging</param>
    Task<string> GetBetExplorerResultsHtmlAsync(string baseLeagueUrl, string? season = null, string? debugSavePath = null)
        => GetHtmlAsync(baseLeagueUrl);

    /// <summary>
    /// Scrapes multiple seasons from BetExplorer in a single browser session.
    /// More efficient - loads page once, then iterates through seasons via dropdown.
    /// </summary>
    /// <param name="baseLeagueUrl">Base league URL like /football/hungary/nb-i/</param>
    /// <param name="seasons">Seasons to scrape, e.g. ["2020-2021", "2019-2020"]</param>
    /// <param name="debugPathPattern">Optional pattern for debug HTML files, use {season} placeholder</param>
    IAsyncEnumerable<(string season, string html)> GetBetExplorerMultiSeasonResultsAsync(
        string baseLeagueUrl,
        IEnumerable<string> seasons,
        string? debugPathPattern = null)
    {
        // Default implementation - fall back to single requests
        return GetMultiSeasonFallbackAsync(baseLeagueUrl, seasons, debugPathPattern);
    }

    private async IAsyncEnumerable<(string season, string html)> GetMultiSeasonFallbackAsync(
        string baseLeagueUrl,
        IEnumerable<string> seasons,
        string? debugPathPattern)
    {
        foreach (var season in seasons)
        {
            var debugPath = debugPathPattern?.Replace("{season}", season);
            var html = await GetBetExplorerResultsHtmlAsync(baseLeagueUrl, season, debugPath);
            yield return (season, html);
        }
    }
}
