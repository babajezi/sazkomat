using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Helpers;
using System.Text.RegularExpressions;

namespace Sazkomat.DataImport.Scrapers;

public class BetExplorerCountryScraper : ICountryScraper
{
    private readonly ResilientHttpClient _httpClient;
    private readonly ILogger<BetExplorerCountryScraper> _logger;

    public BetExplorerCountryScraper(
        ResilientHttpClient httpClient,
        ILogger<BetExplorerCountryScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("betexplorer", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport, List<string>? excludedCountryIds = null)
    {
        var countries = new List<CountryInfo>();

        try
        {
            // Use main BetExplorer homepage which has "All countries" table
            var url = "https://www.betexplorer.com/";

            _logger.LogInformation("Scraping countries from homepage: {Url}", url);

            var html = await _httpClient.GetHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find countries from #countrymenu-home (including both main menu and "More" section)
            // Continents are filtered out by IsInvalidCountryCode method
            var sportSlug = sport.Code.ToLowerInvariant();

            var countryMenuNode = doc.DocumentNode.SelectSingleNode("//article[@id='countrymenu-home']");

            if (countryMenuNode == null)
            {
                _logger.LogWarning("Country menu #countrymenu-home not found on homepage");
                return countries;
            }

            // Find all links that match the pattern /{sport}/{country}/
            var countryLinks = countryMenuNode.SelectNodes(
                $".//a[starts-with(@href, '/{sportSlug}/')]");

            if (countryLinks != null)
            {
                foreach (var link in countryLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    var countryName = link.InnerText.Trim();

                    if (string.IsNullOrEmpty(countryName) || string.IsNullOrEmpty(href))
                        continue;

                    // Extract country code from href
                    // Example: "/football/england/" -> "england"
                    var parts = href.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var countryCode = parts[1];

                        // Skip if it's not a valid country code
                        if (countryCode.Length < 3 || IsInvalidCountryCode(countryCode))
                            continue;

                        // Skip league-specific links (they contain extra slashes)
                        if (parts.Length > 2)
                            continue;

                        // Parse ISO code from SVG image URL
                        // Example: <img src="https://cci.betexplorer.com/gb.svg"> -> ISO code "gb"
                        string? isoCode = null;
                        var imgNode = link.SelectSingleNode(".//img[@src]");
                        if (imgNode != null)
                        {
                            var imgSrc = imgNode.GetAttributeValue("src", "");
                            var match = Regex.Match(imgSrc, @"https://cci\.betexplorer\.com/([a-z]{2})\.svg", RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                isoCode = match.Groups[1].Value.ToLowerInvariant();
                            }
                        }

                        // Fallback to CountryHelper if SVG parsing fails
                        if (string.IsNullOrEmpty(isoCode))
                        {
                            isoCode = CountryHelper.GetIsoCountryCode(countryCode);
                        }

                        // Skip entries without valid ISO code
                        // Real countries have 2-letter ISO codes (gb, us, de, ag for Antigua-Barbuda, etc.)
                        // Continents, international competitions have numbers or no ISO code
                        if (string.IsNullOrEmpty(isoCode))
                        {
                            _logger.LogDebug("Skipping {CountryCode} ({CountryName}) - no valid ISO code (likely continent or competition)",
                                countryCode, countryName);
                            continue;
                        }

                        var flagEmoji = CountryHelper.GetFlagEmoji(isoCode);

                        var country = new CountryInfo
                        {
                            Code = countryCode,
                            Name = NormalizeCountryName(countryName),
                            ProviderCode = countryCode,
                            FlagEmoji = flagEmoji,
                            IsoCode = isoCode ?? ""
                        };

                        // Avoid duplicates
                        if (!countries.Any(c => c.Code.Equals(country.Code, StringComparison.OrdinalIgnoreCase)))
                        {
                            countries.Add(country);
                            _logger.LogDebug("Found country: {Country} (code: {Code}, ISO: {Iso}, flag: {Flag})",
                                country.Name, country.Code, isoCode, flagEmoji);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} countries for {Sport}",
                countries.Count, sport.Name);

            return countries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping countries for {Sport}", sport.Name);
            return countries;
        }
    }

    private bool IsInvalidCountryCode(string code)
    {
        // Filter out common non-country pages and continents
        var invalidCodes = new[] {
            "results", "odds", "fixtures", "standings", "live", "help", "about",
            "popular-bets", "odds-movements", "odds-filter", "livescore",
            "europe", "africa", "asia", "australia-oceania", "oceania",
            "north-central-america", "south-america"
        };
        return invalidCodes.Contains(code.ToLowerInvariant());
    }

    private string NormalizeCountryName(string name)
    {
        // Clean up country name
        return name.Trim()
            .Replace("&nbsp;", " ")
            .Replace("  ", " ");
    }
}
