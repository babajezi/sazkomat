using Sazkomat.Configuration.Entities;

namespace Sazkomat.DataImport.Scrapers;

public interface ISeasonScraper
{
    /// <summary>
    /// Scrapes available seasons for a specific league
    /// </summary>
    /// <param name="league">The league to get seasons for</param>
    /// <returns>List of season identifiers (e.g., "2023-2024")</returns>
    Task<List<string>> ScrapeAvailableSeasonsAsync(League league);

    /// <summary>
    /// Checks if this scraper can handle the given provider
    /// </summary>
    bool CanHandle(DataProvider provider);
}
