using System.Globalization;
using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Scraper for Kingsbet.cz betting provider.
/// Extracts league data from Altenar sportsbook API.
///
/// Kingsbet uses Altenar as their sportsbook provider. The API provides structured data
/// with sports, categories (countries with ISO codes), and championships (leagues).
/// </summary>
public class KingsbetScraper : IBettingProviderScraper
{
    private readonly KingsbetJsonExtractor _jsonExtractor;
    private readonly ILogger<KingsbetScraper> _logger;
    private const string BaseUrl = "https://www.kingsbet.cz";

    public string ProviderCode => "kingsbet";

    public KingsbetScraper(
        KingsbetJsonExtractor jsonExtractor,
        ILogger<KingsbetScraper> logger)
    {
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    public async Task<Result<List<LeagueAvailability>>> GetAvailableLeaguesAsync(string sportCode)
    {
        ArgumentNullException.ThrowIfNull(sportCode);

        try
        {
            _logger.LogInformation("Fetching available leagues from Kingsbet for sport: {SportCode}", sportCode);

            // Map sport code to Altenar sport ID
            var sportId = MapSportCodeToAltenarId(sportCode);
            if (sportId == null)
            {
                return Result<List<LeagueAvailability>>.Failure(
                    $"Sport code '{sportCode}' not supported by Kingsbet scraper");
            }

            // Extract championships from Kingsbet
            var extractResult = await _jsonExtractor.ExtractChampionshipsForSportAsync(sportId.Value);
            if (!extractResult.IsSuccess)
            {
                return Result<List<LeagueAvailability>>.Failure(extractResult.Error ?? "Extraction failed");
            }

            // Transform to LeagueAvailability entities
            var leagues = TransformToLeagueAvailability(extractResult.Value!, sportCode);

            _logger.LogInformation("Successfully extracted {Count} leagues from Kingsbet for {SportCode}",
                leagues.Count, sportCode);

            return Result<List<LeagueAvailability>>.Success(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Kingsbet for sport {SportCode}", sportCode);
            return Result<List<LeagueAvailability>>.Failure($"Failed to scrape Kingsbet: {ex.Message}");
        }
    }

    /// <summary>
    /// Transforms Kingsbet championships to LeagueAvailability entities.
    /// Uses ISO codes from categories for country mapping.
    /// </summary>
    private List<LeagueAvailability> TransformToLeagueAvailability(
        List<Models.KingsbetChampionship> championships,
        string sportCode)
    {
        var leagues = new List<LeagueAvailability>();

        foreach (var champ in championships)
        {
            // Map ISO3 code to BetExplorer-style code
            var (countryCode, countryName) = MapIsoToCountry(champ.CategoryIso, champ.CategoryName);

            leagues.Add(new LeagueAvailability
            {
                ProviderLeagueName = champ.Name,
                ProviderLeagueId = champ.Id.ToString(CultureInfo.InvariantCulture),
                ProviderUrl = $"{BaseUrl}/sport/category/{champ.CategoryId}/champ/{champ.Id}",
                SportCode = sportCode,
                CountryCode = countryCode,
                CountryName = countryName
            });
        }

        _logger.LogDebug("Transformed {Count} championships to LeagueAvailability", leagues.Count);

        return leagues;
    }

    /// <summary>
    /// Maps internal sport code to Altenar sport ID
    /// </summary>
    private static int? MapSportCodeToAltenarId(string sportCode)
    {
        return sportCode.ToLowerInvariant() switch
        {
            "football" => AltenarSportId.Football,
            "hockey" => AltenarSportId.Hockey,
            "basketball" => AltenarSportId.Basketball,
            "tennis" => AltenarSportId.Tennis,
            "handball" => AltenarSportId.Handball,
            "volleyball" => AltenarSportId.Volleyball,
            _ => null
        };
    }

    /// <summary>
    /// Maps ISO 3166-1 alpha-3 code to BetExplorer-style country code and name.
    /// Categories without ISO codes (international competitions) are skipped.
    /// </summary>
    private (string? Code, string? Name) MapIsoToCountry(string? iso, string? categoryName)
    {
        if (string.IsNullOrEmpty(iso))
        {
            // International competitions (Evropa, Svět, etc.) - use category name as fallback
            return (null, categoryName);
        }

        // Map ISO3 to BetExplorer-style codes
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

        _logger.LogDebug("Unknown ISO code: {Iso}, using category name: {Name}", iso, categoryName);
        return (iso.ToLowerInvariant(), categoryName);
    }
}

/// <summary>
/// Altenar sport IDs used by Kingsbet
/// </summary>
public static class AltenarSportId
{
    public const int Football = 66;
    public const int Basketball = 67;
    public const int Tennis = 68;
    public const int Volleyball = 69;
    public const int Hockey = 70;
    public const int Handball = 73;
}
