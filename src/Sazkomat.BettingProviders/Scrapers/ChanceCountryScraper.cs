using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Country scraper for Chance.cz betting provider.
/// Extracts unique countries from available leagues since Chance doesn't have
/// a dedicated country endpoint (countries are derived from league names).
/// </summary>
public class ChanceCountryScraper : ICountryScraper
{
    private readonly ChanceScraper _chanceScraper;
    private readonly ILogger<ChanceCountryScraper> _logger;

    public ChanceCountryScraper(
        ChanceScraper chanceScraper,
        ILogger<ChanceCountryScraper> logger)
    {
        _chanceScraper = chanceScraper;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("chance", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport, List<string>? excludedCountryIds = null)
    {
        var sportCode = MapSportToCode(sport);
        if (sportCode == null)
        {
            _logger.LogWarning("Sport {SportName} not supported by Chance scraper", sport.Name);
            return new List<CountryInfo>();
        }

        _logger.LogInformation("Scraping countries from Chance for sport {SportCode}", sportCode);

        // Get all leagues from Chance - countries are derived from league names
        var leaguesResult = await _chanceScraper.GetAvailableLeaguesAsync(sportCode);
        if (!leaguesResult.IsSuccess)
        {
            _logger.LogError("Failed to get leagues from Chance: {Error}", leaguesResult.Error);
            return new List<CountryInfo>();
        }

        var leagues = leaguesResult.Value!;

        // Extract unique countries from leagues
        var countryMap = new Dictionary<string, (string Code, string Name)>(StringComparer.OrdinalIgnoreCase);

        foreach (var league in leagues)
        {
            if (!string.IsNullOrEmpty(league.CountryCode) && !string.IsNullOrEmpty(league.CountryName))
            {
                if (!countryMap.ContainsKey(league.CountryCode))
                {
                    countryMap[league.CountryCode] = (league.CountryCode, league.CountryName);
                }
            }
        }

        // Filter out excluded countries if provided
        if (excludedCountryIds != null && excludedCountryIds.Any())
        {
            var originalCount = countryMap.Count;
            foreach (var excluded in excludedCountryIds)
            {
                countryMap.Remove(excluded);
            }

            _logger.LogInformation("Filtered out {Excluded} countries based on ExcludedCountryIds configuration",
                originalCount - countryMap.Count);
        }

        // Transform to CountryInfo
        var countries = countryMap.Values
            .Select(c => new CountryInfo
            {
                Code = c.Code,
                Name = c.Name,
                ProviderCode = c.Code,
                FlagEmoji = GetFlagEmoji(c.Code),
                IsoCode = GetIsoCode(c.Code)
            })
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Found {Count} unique countries from Chance leagues for sport {SportCode}",
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
            "hockey" => "hockey",
            "hokej" => "hockey",
            "basketball" => "basketball",
            "tennis" => "tennis",
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
