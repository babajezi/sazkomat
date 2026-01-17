using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Country scraper for Kingsbet - extracts countries from Altenar API categories.
/// Categories with ISO codes are real countries, those without are international competitions.
/// </summary>
public class KingsbetCountryScraper : ICountryScraper
{
    private readonly KingsbetJsonExtractor _jsonExtractor;
    private readonly ILogger<KingsbetCountryScraper> _logger;

    public KingsbetCountryScraper(
        KingsbetJsonExtractor jsonExtractor,
        ILogger<KingsbetCountryScraper> logger)
    {
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    public bool CanHandle(DataProvider provider)
    {
        return provider.Code.Equals("kingsbet", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<CountryInfo>> ScrapeCountriesAsync(Sport sport, List<string>? excludedCountryIds = null)
    {
        _logger.LogInformation("Scraping countries from Kingsbet for {Sport}", sport.Name);

        // Map sport to Altenar ID
        var sportId = MapSportToAltenarId(sport);
        if (sportId == null)
        {
            _logger.LogWarning("Sport {SportName} not supported by Kingsbet scraper", sport.Name);
            return new List<CountryInfo>();
        }

        // Get categories from Kingsbet
        var categoriesResult = await _jsonExtractor.ExtractCategoriesForSportAsync(sportId.Value);
        if (!categoriesResult.IsSuccess)
        {
            _logger.LogError("Failed to get Kingsbet categories: {Error}", categoriesResult.Error);
            return new List<CountryInfo>();
        }

        var categories = categoriesResult.Value!;

        // Filter to categories with ISO codes (real countries)
        var countryCandidates = categories
            .Where(c => !string.IsNullOrEmpty(c.Iso))
            .ToList();

        // Log categories without ISO codes (international competitions)
        var internationalCount = categories.Count - countryCandidates.Count;
        if (internationalCount > 0)
        {
            _logger.LogDebug("Skipping {Count} international categories (no ISO code)", internationalCount);
        }

        // Convert to CountryInfo with ISO mapping
        var countries = new List<CountryInfo>();

        foreach (var cat in countryCandidates)
        {
            var (countryCode, countryName) = MapIsoToCountry(cat.Iso!, cat.Name);

            // Skip if excluded
            if (excludedCountryIds != null && excludedCountryIds.Contains(countryCode))
            {
                continue;
            }

            countries.Add(new CountryInfo
            {
                Code = countryCode,
                Name = countryName,
                ProviderCode = cat.Iso!,
                IsoCode = cat.Iso
            });
        }

        // Remove duplicates (same country code)
        var uniqueCountries = countries
            .GroupBy(c => c.Code)
            .Select(g => g.First())
            .OrderBy(c => c.Name)
            .ToList();

        _logger.LogInformation("Found {Count} unique countries from Kingsbet for sport {SportName}",
            uniqueCountries.Count, sport.Name);

        return uniqueCountries;
    }

    private static int? MapSportToAltenarId(Sport sport)
    {
        var code = sport.Code?.ToLowerInvariant();
        return code switch
        {
            "football" => AltenarSportId.Football,
            "fotbal" => AltenarSportId.Football,
            "hockey" => AltenarSportId.Hockey,
            "hokej" => AltenarSportId.Hockey,
            "basketball" => AltenarSportId.Basketball,
            "basketbal" => AltenarSportId.Basketball,
            "tennis" => AltenarSportId.Tennis,
            "tenis" => AltenarSportId.Tennis,
            "handball" => AltenarSportId.Handball,
            "hazena" => AltenarSportId.Handball,
            "volleyball" => AltenarSportId.Volleyball,
            "volejbal" => AltenarSportId.Volleyball,
            _ => null
        };
    }

    /// <summary>
    /// Maps ISO 3166-1 alpha-3 code to BetExplorer-style country code and name.
    /// </summary>
    private static (string Code, string Name) MapIsoToCountry(string iso, string categoryName)
    {
        // Same mapping as in KingsbetScraper
        var isoMapping = new Dictionary<string, (string Code, string Name)>(StringComparer.OrdinalIgnoreCase)
        {
            // Europe
            { "CZE", ("czech-republic", "Česko") },
            { "ENG", ("england", "Anglie") },
            { "DEU", ("germany", "Německo") },
            { "ESP", ("spain", "Španělsko") },
            { "ITA", ("italy", "Itálie") },
            { "FRA", ("france", "Francie") },
            { "NLD", ("netherlands", "Nizozemsko") },
            { "BEL", ("belgium", "Belgie") },
            { "PRT", ("portugal", "Portugalsko") },
            { "AUT", ("austria", "Rakousko") },
            { "CHE", ("switzerland", "Švýcarsko") },
            { "POL", ("poland", "Polsko") },
            { "GRC", ("greece", "Řecko") },
            { "TUR", ("turkey", "Turecko") },
            { "RUS", ("russia", "Rusko") },
            { "UKR", ("ukraine", "Ukrajina") },
            { "SCO", ("scotland", "Skotsko") },
            { "WAL", ("wales", "Wales") },
            { "NIR", ("northern-ireland", "Severní Irsko") },
            { "IRL", ("ireland", "Irsko") },
            { "DNK", ("denmark", "Dánsko") },
            { "NOR", ("norway", "Norsko") },
            { "SWE", ("sweden", "Švédsko") },
            { "FIN", ("finland", "Finsko") },
            { "ISL", ("iceland", "Island") },
            { "HRV", ("croatia", "Chorvatsko") },
            { "SRB", ("serbia", "Srbsko") },
            { "HUN", ("hungary", "Maďarsko") },
            { "ROU", ("romania", "Rumunsko") },
            { "BGR", ("bulgaria", "Bulharsko") },
            { "SVK", ("slovakia", "Slovensko") },
            { "SVN", ("slovenia", "Slovinsko") },
            { "BIH", ("bosnia-herzegovina", "Bosna a Hercegovina") },
            { "MNE", ("montenegro", "Černá Hora") },
            { "MKD", ("north-macedonia", "Severní Makedonie") },
            { "ALB", ("albania", "Albánie") },
            { "XKX", ("kosovo", "Kosovo") },
            { "CYP", ("cyprus", "Kypr") },
            { "MLT", ("malta", "Malta") },
            { "LUX", ("luxembourg", "Lucembursko") },
            { "LVA", ("latvia", "Lotyšsko") },
            { "LTU", ("lithuania", "Litva") },
            { "EST", ("estonia", "Estonsko") },
            { "BLR", ("belarus", "Bělorusko") },
            { "GEO", ("georgia", "Gruzie") },
            { "ARM", ("armenia", "Arménie") },
            { "AZE", ("azerbaijan", "Ázerbájdžán") },
            { "KAZ", ("kazakhstan", "Kazachstán") },
            { "AND", ("andorra", "Andorra") },
            { "SMR", ("san-marino", "San Marino") },
            { "LIE", ("liechtenstein", "Lichtenštejnsko") },
            { "MDA", ("moldova", "Moldavsko") },
            { "FRO", ("faroe-islands", "Faerské ostrovy") },
            { "GIB", ("gibraltar", "Gibraltar") },

            // Americas
            { "USA", ("usa", "USA") },
            { "CAN", ("canada", "Kanada") },
            { "MEX", ("mexico", "Mexiko") },
            { "BRA", ("brazil", "Brazílie") },
            { "ARG", ("argentina", "Argentina") },
            { "CHL", ("chile", "Chile") },
            { "COL", ("colombia", "Kolumbie") },
            { "PER", ("peru", "Peru") },
            { "URY", ("uruguay", "Uruguay") },
            { "PRY", ("paraguay", "Paraguay") },
            { "ECU", ("ecuador", "Ekvádor") },
            { "BOL", ("bolivia", "Bolívie") },
            { "VEN", ("venezuela", "Venezuela") },
            { "CRI", ("costa-rica", "Kostarika") },
            { "HND", ("honduras", "Honduras") },
            { "GTM", ("guatemala", "Guatemala") },
            { "SLV", ("el-salvador", "Salvador") },
            { "PAN", ("panama", "Panama") },
            { "JAM", ("jamaica", "Jamajka") },
            { "NIC", ("nicaragua", "Nikaragua") },
            { "DOM", ("dominican-republic", "Dominikánská republika") },

            // Asia
            { "JPN", ("japan", "Japonsko") },
            { "KOR", ("south-korea", "Jižní Korea") },
            { "CHN", ("china", "Čína") },
            { "THA", ("thailand", "Thajsko") },
            { "VNM", ("vietnam", "Vietnam") },
            { "IDN", ("indonesia", "Indonésie") },
            { "MYS", ("malaysia", "Malajsie") },
            { "SGP", ("singapore", "Singapur") },
            { "HKG", ("hong-kong", "Hongkong") },
            { "IND", ("india", "Indie") },
            { "IRN", ("iran", "Írán") },
            { "SAU", ("saudi-arabia", "Saúdská Arábie") },
            { "ARE", ("uae", "SAE") },
            { "QAT", ("qatar", "Katar") },
            { "OMN", ("oman", "Omán") },
            { "KWT", ("kuwait", "Kuvajt") },
            { "BHR", ("bahrain", "Bahrajn") },
            { "UZB", ("uzbekistan", "Uzbekistán") },
            { "ISR", ("israel", "Izrael") },
            { "LBN", ("lebanon", "Libanon") },
            { "JOR", ("jordan", "Jordánsko") },
            { "IRQ", ("iraq", "Irák") },
            { "SYR", ("syria", "Sýrie") },
            { "BGD", ("bangladesh", "Bangladéš") },
            { "KHM", ("cambodia", "Kambodža") },
            { "TWN", ("taiwan", "Tchaj-wan") },
            { "PHL", ("philippines", "Filipíny") },
            { "MMR", ("myanmar", "Myanmar") },

            // Oceania
            { "AUS", ("australia", "Austrálie") },
            { "NZL", ("new-zealand", "Nový Zéland") },

            // Africa
            { "EGY", ("egypt", "Egypt") },
            { "MAR", ("morocco", "Maroko") },
            { "TUN", ("tunisia", "Tunisko") },
            { "DZA", ("algeria", "Alžírsko") },
            { "ZAF", ("south-africa", "Jižní Afrika") },
            { "NGA", ("nigeria", "Nigérie") },
            { "GHA", ("ghana", "Ghana") },
            { "CIV", ("ivory-coast", "Pobřeží slonoviny") },
            { "SEN", ("senegal", "Senegal") },
            { "CMR", ("cameroon", "Kamerun") },
            { "KEN", ("kenya", "Keňa") },
            { "TZA", ("tanzania", "Tanzanie") },
            { "UGA", ("uganda", "Uganda") },
            { "ETH", ("ethiopia", "Etiopie") },
            { "RWA", ("rwanda", "Rwanda") },
            { "ZMB", ("zambia", "Zambie") },
            { "ZWE", ("zimbabwe", "Zimbabwe") },
            { "AGO", ("angola", "Angola") },
            { "MOZ", ("mozambique", "Mosambik") },
            { "GAB", ("gabon", "Gabon") },
            { "COD", ("dr-congo", "DR Kongo") },
            { "BFA", ("burkina-faso", "Burkina Faso") },
            { "MLI", ("mali", "Mali") },
            { "GIN", ("guinea", "Guinea") },
            { "BEN", ("benin", "Benin") },
            { "TGO", ("togo", "Togo") },
            { "LBY", ("libya", "Libye") },
            { "SDN", ("sudan", "Súdán") },
            { "MUS", ("mauritius", "Mauricius") },
            { "CPV", ("cape-verde", "Kapverdy") },
            { "MRT", ("mauritania", "Mauritánie") }
        };

        if (isoMapping.TryGetValue(iso, out var mapping))
        {
            return (mapping.Code, mapping.Name);
        }

        // Fallback - use ISO as code and category name
        return (iso.ToLowerInvariant(), categoryName);
    }
}
