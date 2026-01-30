using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Scrapers;

public interface ILeagueScraper
{
    Task<ScrapeResult> ScrapeSeasonAsync(League league, string season);

    /// <summary>
    /// Scrapes multiple seasons from BetExplorer in a single browser session.
    /// More efficient - loads page once, then iterates through seasons via dropdown.
    /// </summary>
    Task<Dictionary<string, ScrapeResult>> ScrapeMultipleSeasonsAsync(League league, IEnumerable<string> seasons)
    {
        // Default implementation - call ScrapeSeasonAsync for each season
        return ScrapeMultipleSeasonsDefaultAsync(league, seasons);
    }

    private async Task<Dictionary<string, ScrapeResult>> ScrapeMultipleSeasonsDefaultAsync(
        League league, IEnumerable<string> seasons)
    {
        var results = new Dictionary<string, ScrapeResult>();
        foreach (var season in seasons)
        {
            results[season] = await ScrapeSeasonAsync(league, season);
        }
        return results;
    }

    bool CanHandle(Sport sport);
}
