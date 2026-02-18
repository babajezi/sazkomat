using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Country scraper for Fortuna.cz betting provider.
/// Extracts unique countries from the Fortuna football page leagues.
/// </summary>
public class FortunaCountryScraper : ICountryScraper
{
    private readonly FortunaScraper _fortunaScraper;
    private readonly ILogger<FortunaCountryScraper> _logger;

    public FortunaCountryScraper(
        FortunaScraper fortunaScraper,
        ILogger<FortunaCountryScraper> logger)
    {
        _fortunaScraper = fortunaScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("fortuna", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport, List<string>? excludedCountryIds = null)
    {
        var sportCode = MapSportToCode(sport);
        if (sportCode == null)
        {
            _logger.LogWarning("Sport {SportName} not supported by Fortuna scraper", sport.Name);
            return new List<CountryInfo>();
        }

        _logger.LogInformation("Scraping countries from Fortuna for sport {SportCode}", sportCode);

        // Get all regions (countries) from Fortuna
        var regionsResult = await _fortunaScraper.GetAvailableRegionsAsync(sportCode);
        if (!regionsResult.IsSuccess)
        {
            _logger.LogError("Failed to get regions from Fortuna: {Error}", regionsResult.Error);
            return new List<CountryInfo>();
        }

        var regions = regionsResult.Value!;

        // Filter out excluded countries if provided
        if (excludedCountryIds != null && excludedCountryIds.Any())
        {
            var originalCount = regions.Count;
            regions = regions
                .Where(r => !excludedCountryIds.Contains(r.Code))
                .ToList();

            _logger.LogInformation("Filtered out {Excluded} countries based on ExcludedCountryIds configuration",
                originalCount - regions.Count);
        }

        // Transform to CountryInfo
        var countries = regions
            .Select(r => new CountryInfo
            {
                Code = r.Code,
                Name = r.Name,
                ProviderCode = r.Code,
                FlagEmoji = GetFlagEmoji(r.Code),
                IsoCode = GetIsoCode(r.Code)
            })
            .ToList();

        _logger.LogInformation("Found {Count} countries from Fortuna for sport {SportCode}",
            countries.Count, sportCode);

        return countries;
    }

    /// <summary>
    /// Maps Sport entity to sport code.
    /// </summary>
    private static string? MapSportToCode(Sport sport)
    {
        return sport.Code?.ToLowerInvariant() switch
        {
            "football" => "football",
            "fotbal" => "football",
            _ => null
        };
    }

    /// <summary>
    /// Gets flag emoji for a country code.
    /// </summary>
    private static string? GetFlagEmoji(string countryCode)
    {
        return countryCode.ToLowerInvariant() switch
        {
            "england" => "🏴󠁧󠁢󠁥󠁮󠁧󠁿",
            "scotland" => "🏴󠁧󠁢󠁳󠁣󠁴󠁿",
            "wales" => "🏴󠁧󠁢󠁷󠁬󠁳󠁿",
            "northern-ireland" => "🇬🇧",
            "germany" => "🇩🇪",
            "spain" => "🇪🇸",
            "italy" => "🇮🇹",
            "france" => "🇫🇷",
            "netherlands" => "🇳🇱",
            "belgium" => "🇧🇪",
            "portugal" => "🇵🇹",
            "austria" => "🇦🇹",
            "switzerland" => "🇨🇭",
            "poland" => "🇵🇱",
            "czech-republic" => "🇨🇿",
            "slovakia" => "🇸🇰",
            "greece" => "🇬🇷",
            "turkey" => "🇹🇷",
            "russia" => "🇷🇺",
            "ukraine" => "🇺🇦",
            "ireland" => "🇮🇪",
            "denmark" => "🇩🇰",
            "norway" => "🇳🇴",
            "sweden" => "🇸🇪",
            "finland" => "🇫🇮",
            "croatia" => "🇭🇷",
            "serbia" => "🇷🇸",
            "hungary" => "🇭🇺",
            "romania" => "🇷🇴",
            "bulgaria" => "🇧🇬",
            "slovenia" => "🇸🇮",
            "bosnia-herzegovina" => "🇧🇦",
            "montenegro" => "🇲🇪",
            "north-macedonia" => "🇲🇰",
            "albania" => "🇦🇱",
            "cyprus" => "🇨🇾",
            "israel" => "🇮🇱",
            "usa" => "🇺🇸",
            "brazil" => "🇧🇷",
            "argentina" => "🇦🇷",
            "mexico" => "🇲🇽",
            "japan" => "🇯🇵",
            "south-korea" => "🇰🇷",
            "australia" => "🇦🇺",
            _ => null
        };
    }

    /// <summary>
    /// Gets ISO 3166-1 alpha-2 code for a country.
    /// </summary>
    private static string? GetIsoCode(string countryCode)
    {
        return countryCode.ToLowerInvariant() switch
        {
            "england" => "GB-ENG",
            "scotland" => "GB-SCT",
            "wales" => "GB-WLS",
            "northern-ireland" => "GB-NIR",
            "germany" => "DE",
            "spain" => "ES",
            "italy" => "IT",
            "france" => "FR",
            "netherlands" => "NL",
            "belgium" => "BE",
            "portugal" => "PT",
            "austria" => "AT",
            "switzerland" => "CH",
            "poland" => "PL",
            "czech-republic" => "CZ",
            "slovakia" => "SK",
            "greece" => "GR",
            "turkey" => "TR",
            "russia" => "RU",
            "ukraine" => "UA",
            "ireland" => "IE",
            "denmark" => "DK",
            "norway" => "NO",
            "sweden" => "SE",
            "finland" => "FI",
            "croatia" => "HR",
            "serbia" => "RS",
            "hungary" => "HU",
            "romania" => "RO",
            "bulgaria" => "BG",
            "slovenia" => "SI",
            "bosnia-herzegovina" => "BA",
            "montenegro" => "ME",
            "north-macedonia" => "MK",
            "albania" => "AL",
            "cyprus" => "CY",
            "israel" => "IL",
            "usa" => "US",
            "brazil" => "BR",
            "argentina" => "AR",
            "mexico" => "MX",
            "japan" => "JP",
            "south-korea" => "KR",
            "australia" => "AU",
            _ => null
        };
    }
}
