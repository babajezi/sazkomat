using Sazkomat.Configuration.Entities;

namespace Sazkomat.DataImport.Scrapers;

public interface ICountryScraper
{
    /// <summary>
    /// Scrapes available countries from the provider for a specific sport
    /// </summary>
    /// <param name="sport">The sport to get countries for</param>
    /// <returns>List of country codes and names</returns>
    Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport);

    /// <summary>
    /// Checks if this scraper can handle the given provider
    /// </summary>
    bool CanHandle(DataProvider provider);
}

public class CountryInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ProviderCode { get; set; }
    public string? FlagEmoji { get; set; }
    public string? IsoCode { get; set; }
}
