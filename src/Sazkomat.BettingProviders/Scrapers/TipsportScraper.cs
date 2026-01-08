using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Entities;
using Sazkomat.BettingProviders.Models;
using Sazkomat.BettingProviders.Services;
using Sazkomat.Core.Common;
using Sazkomat.DataImport.Services;

namespace Sazkomat.BettingProviders.Scrapers;

/// <summary>
/// Scraper for Tipsport.cz betting provider.
/// Extracts league data from REST API via Playwright (to bypass Cloudflare).
///
/// Note: Tipsport uses Czech names without country hierarchy (e.g., "1. anglická liga").
/// Country mapping is resolved via ICountryMappingService (database-driven) with fallback to hardcoded dictionary.
/// </summary>
public class TipsportScraper : IBettingProviderScraper
{
    private readonly TipsportJsonExtractor _jsonExtractor;
    private readonly ICountryMappingService _countryMappingService;
    private readonly ILogger<TipsportScraper> _logger;
    private const string BaseUrl = "https://www.tipsport.cz";

    public string ProviderCode => "tipsport";

    public TipsportScraper(
        TipsportJsonExtractor jsonExtractor,
        ICountryMappingService countryMappingService,
        ILogger<TipsportScraper> logger)
    {
        _jsonExtractor = jsonExtractor;
        _countryMappingService = countryMappingService;
        _logger = logger;
    }

    public async Task<Result<List<LeagueAvailability>>> GetAvailableLeaguesAsync(string sportCode)
    {
        try
        {
            _logger.LogInformation("Fetching available leagues from Tipsport for sport: {SportCode}", sportCode);

            // Map sport code to Tipsport SuperSportId
            var superSportId = MapSportCodeToSuperSportId(sportCode);
            if (superSportId == null)
            {
                return Result<List<LeagueAvailability>>.Failure(
                    $"Sport code '{sportCode}' not supported by Tipsport scraper");
            }

            // Extract competitions from Tipsport
            var extractResult = await _jsonExtractor.ExtractCompetitionsForSportAsync(superSportId.Value);
            if (!extractResult.IsSuccess)
            {
                return Result<List<LeagueAvailability>>.Failure(extractResult.Error);
            }

            // Transform to LeagueAvailability entities
            var leagues = await TransformToLeagueAvailabilityAsync(extractResult.Value, sportCode);

            _logger.LogInformation("Successfully extracted {Count} leagues from Tipsport for {SportCode}",
                leagues.Count, sportCode);

            return Result<List<LeagueAvailability>>.Success(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping Tipsport for sport {SportCode}", sportCode);
            return Result<List<LeagueAvailability>>.Failure($"Failed to scrape Tipsport: {ex.Message}");
        }
    }

    /// <summary>
    /// Transforms Tipsport competitions to LeagueAvailability entities.
    /// Uses ICountryMappingService (database) to resolve country codes, with fallback to hardcoded dictionary.
    /// </summary>
    private async Task<List<LeagueAvailability>> TransformToLeagueAvailabilityAsync(
        List<TipsportCompetition> competitions,
        string sportCode)
    {
        var leagues = new List<LeagueAvailability>();

        foreach (var comp in competitions)
        {
            // Try to derive country from competition title using database mappings
            var (countryCode, countryName) = await DeriveCountryFromTitleAsync(comp.Title);

            leagues.Add(new LeagueAvailability
            {
                ProviderLeagueName = comp.Title,
                ProviderLeagueId = comp.Id.ToString(),
                ProviderUrl = $"{BaseUrl}{comp.Url}",
                SportCode = sportCode,
                CountryCode = countryCode,
                CountryName = countryName
            });
        }

        _logger.LogDebug("Transformed {Count} competitions to LeagueAvailability", leagues.Count);

        return leagues;
    }

    /// <summary>
    /// Derives country code and name from Czech competition title.
    /// Uses ICountryMappingService (database) first, then falls back to hardcoded dictionary.
    /// </summary>
    private async Task<(string? Code, string? Name)> DeriveCountryFromTitleAsync(string title)
    {
        // Try database mappings first
        var (code, name) = await _countryMappingService.ResolveCountryAsync(ProviderCode, title);
        if (code != null)
        {
            return (code, name);
        }

        // Fall back to hardcoded dictionary (for forward compatibility)
        return DeriveCountryFromTitleFallback(title);
    }

    /// <summary>
    /// Maps internal sport code to Tipsport SuperSportId
    /// </summary>
    private static int? MapSportCodeToSuperSportId(string sportCode)
    {
        return sportCode.ToLowerInvariant() switch
        {
            "football" => TipsportSuperSportId.Football,
            "hockey" => TipsportSuperSportId.Hockey,
            "basketball" => TipsportSuperSportId.Basketball,
            "tennis" => TipsportSuperSportId.Tennis,
            "handball" => TipsportSuperSportId.Handball,
            "volleyball" => TipsportSuperSportId.Volleyball,
            _ => null
        };
    }

    /// <summary>
    /// Fallback: Attempts to derive country code and name from Czech competition title.
    /// E.g., "1. anglická liga" -> ("england", "Anglie")
    /// Used when database mappings don't have a match (forward compatibility).
    /// </summary>
    private static (string? Code, string? Name) DeriveCountryFromTitleFallback(string title)
    {
        // Common patterns in Tipsport competition names
        var countryMappings = new Dictionary<string, (string Code, string Name)>(StringComparer.OrdinalIgnoreCase)
        {
            // European countries
            { "anglická", ("england", "Anglie") },
            { "anglický", ("england", "Anglie") },
            { "německá", ("germany", "Německo") },
            { "německý", ("germany", "Německo") },
            { "španělská", ("spain", "Španělsko") },
            { "španělský", ("spain", "Španělsko") },
            { "italská", ("italy", "Itálie") },
            { "italský", ("italy", "Itálie") },
            { "francouzská", ("france", "Francie") },
            { "francouzský", ("france", "Francie") },
            { "nizozemská", ("netherlands", "Nizozemsko") },
            { "nizozemský", ("netherlands", "Nizozemsko") },
            { "belgická", ("belgium", "Belgie") },
            { "belgický", ("belgium", "Belgie") },
            { "portugalská", ("portugal", "Portugalsko") },
            { "portugalský", ("portugal", "Portugalsko") },
            { "rakouská", ("austria", "Rakousko") },
            { "rakouský", ("austria", "Rakousko") },
            { "švýcarská", ("switzerland", "Švýcarsko") },
            { "švýcarský", ("switzerland", "Švýcarsko") },
            { "polská", ("poland", "Polsko") },
            { "polský", ("poland", "Polsko") },
            { "řecká", ("greece", "Řecko") },
            { "řecký", ("greece", "Řecko") },
            { "turecká", ("turkey", "Turecko") },
            { "turecký", ("turkey", "Turecko") },
            { "ruská", ("russia", "Rusko") },
            { "ruský", ("russia", "Rusko") },
            { "ukrajinská", ("ukraine", "Ukrajina") },
            { "ukrajinský", ("ukraine", "Ukrajina") },
            { "skotská", ("scotland", "Skotsko") },
            { "skotský", ("scotland", "Skotsko") },
            { "velšská", ("wales", "Wales") },
            { "velšský", ("wales", "Wales") },
            { "dánská", ("denmark", "Dánsko") },
            { "dánský", ("denmark", "Dánsko") },
            { "norská", ("norway", "Norsko") },
            { "norský", ("norway", "Norsko") },
            { "švédská", ("sweden", "Švédsko") },
            { "švédský", ("sweden", "Švédsko") },
            { "finská", ("finland", "Finsko") },
            { "finský", ("finland", "Finsko") },
            { "chorvatská", ("croatia", "Chorvatsko") },
            { "chorvatský", ("croatia", "Chorvatsko") },
            { "srbská", ("serbia", "Srbsko") },
            { "srbský", ("serbia", "Srbsko") },
            { "maďarská", ("hungary", "Maďarsko") },
            { "maďarský", ("hungary", "Maďarsko") },
            { "rumunská", ("romania", "Rumunsko") },
            { "rumunský", ("romania", "Rumunsko") },
            { "bulharská", ("bulgaria", "Bulharsko") },
            { "bulharský", ("bulgaria", "Bulharsko") },
            { "kyperská", ("cyprus", "Kypr") },
            { "kyperský", ("cyprus", "Kypr") },
            { "izraelská", ("israel", "Izrael") },
            { "izraelský", ("israel", "Izrael") },
            { "severoirská", ("northern-ireland", "Severní Irsko") },
            { "severoirský", ("northern-ireland", "Severní Irsko") },
            { "irská", ("ireland", "Irsko") },
            { "irský", ("ireland", "Irsko") },
            { "islandská", ("iceland", "Island") },
            { "islandský", ("iceland", "Island") },
            { "slovinská", ("slovenia", "Slovinsko") },
            { "slovinský", ("slovenia", "Slovinsko") },
            { "bosenská", ("bosnia-herzegovina", "Bosna a Hercegovina") },
            { "bosenský", ("bosnia-herzegovina", "Bosna a Hercegovina") },
            { "černohorská", ("montenegro", "Černá Hora") },
            { "černohorský", ("montenegro", "Černá Hora") },
            { "makedonská", ("north-macedonia", "Severní Makedonie") },
            { "makedonský", ("north-macedonia", "Severní Makedonie") },
            { "albánská", ("albania", "Albánie") },
            { "albánský", ("albania", "Albánie") },
            { "kosovská", ("kosovo", "Kosovo") },
            { "kosovský", ("kosovo", "Kosovo") },
            { "gruzínská", ("georgia", "Gruzie") },
            { "gruzínský", ("georgia", "Gruzie") },
            { "ázerbájdžánská", ("azerbaijan", "Ázerbájdžán") },
            { "ázerbájdžánský", ("azerbaijan", "Ázerbájdžán") },
            { "arménská", ("armenia", "Arménie") },
            { "arménský", ("armenia", "Arménie") },
            { "kazašská", ("kazakhstan", "Kazachstán") },
            { "kazašský", ("kazakhstan", "Kazachstán") },
            { "běloruská", ("belarus", "Bělorusko") },
            { "běloruský", ("belarus", "Bělorusko") },
            { "litevská", ("lithuania", "Litva") },
            { "litevský", ("lithuania", "Litva") },
            { "lotyšská", ("latvia", "Lotyšsko") },
            { "lotyšský", ("latvia", "Lotyšsko") },
            { "estonská", ("estonia", "Estonsko") },
            { "estonský", ("estonia", "Estonsko") },
            { "maltská", ("malta", "Malta") },
            { "maltský", ("malta", "Malta") },
            { "lucemburská", ("luxembourg", "Lucembursko") },
            { "lucemburský", ("luxembourg", "Lucembursko") },

            // Americas
            { "americká", ("usa", "USA") },
            { "americký", ("usa", "USA") },
            { "argentinská", ("argentina", "Argentina") },
            { "argentinský", ("argentina", "Argentina") },
            { "brazilská", ("brazil", "Brazílie") },
            { "brazilský", ("brazil", "Brazílie") },
            { "mexická", ("mexico", "Mexiko") },
            { "mexický", ("mexico", "Mexiko") },
            { "chilská", ("chile", "Chile") },
            { "chilský", ("chile", "Chile") },
            { "kolumbijská", ("colombia", "Kolumbie") },
            { "kolumbijský", ("colombia", "Kolumbie") },
            { "peruánská", ("peru", "Peru") },
            { "peruánský", ("peru", "Peru") },
            { "uruguayská", ("uruguay", "Uruguay") },
            { "uruguayský", ("uruguay", "Uruguay") },
            { "paraguayská", ("paraguay", "Paraguay") },
            { "paraguayský", ("paraguay", "Paraguay") },
            { "ekvádorská", ("ecuador", "Ekvádor") },
            { "ekvádorský", ("ecuador", "Ekvádor") },
            { "bolívijská", ("bolivia", "Bolívie") },
            { "bolívijský", ("bolivia", "Bolívie") },
            { "venezuelská", ("venezuela", "Venezuela") },
            { "venezuelský", ("venezuela", "Venezuela") },
            { "kostarická", ("costa-rica", "Kostarika") },
            { "kostarický", ("costa-rica", "Kostarika") },
            { "honduraská", ("honduras", "Honduras") },
            { "honduraský", ("honduras", "Honduras") },
            { "guatemalská", ("guatemala", "Guatemala") },
            { "guatemalský", ("guatemala", "Guatemala") },
            { "salvadorská", ("el-salvador", "Salvador") },
            { "salvadorský", ("el-salvador", "Salvador") },
            { "panamská", ("panama", "Panama") },
            { "panamský", ("panama", "Panama") },
            { "jamajská", ("jamaica", "Jamajka") },
            { "jamajský", ("jamaica", "Jamajka") },
            { "nikaragujská", ("nicaragua", "Nikaragua") },
            { "nikaragujský", ("nicaragua", "Nikaragua") },
            { "dominikánská", ("dominican-republic", "Dominikánská republika") },
            { "dominikánský", ("dominican-republic", "Dominikánská republika") },
            { "kanadská", ("canada", "Kanada") },
            { "kanadský", ("canada", "Kanada") },

            // Asia & Oceania
            { "japonská", ("japan", "Japonsko") },
            { "japonský", ("japan", "Japonsko") },
            { "korejská", ("south-korea", "Jižní Korea") },
            { "korejský", ("south-korea", "Jižní Korea") },
            { "čínská", ("china", "Čína") },
            { "čínský", ("china", "Čína") },
            { "australská", ("australia", "Austrálie") },
            { "australský", ("australia", "Austrálie") },
            { "novozélandská", ("new-zealand", "Nový Zéland") },
            { "novozélandský", ("new-zealand", "Nový Zéland") },
            { "thajská", ("thailand", "Thajsko") },
            { "thajský", ("thailand", "Thajsko") },
            { "vietnamská", ("vietnam", "Vietnam") },
            { "vietnamský", ("vietnam", "Vietnam") },
            { "indonéská", ("indonesia", "Indonésie") },
            { "indonéský", ("indonesia", "Indonésie") },
            { "malajská", ("malaysia", "Malajsie") },
            { "malajský", ("malaysia", "Malajsie") },
            { "malajsijská", ("malaysia", "Malajsie") },
            { "malajsijský", ("malaysia", "Malajsie") },
            { "singapurská", ("singapore", "Singapur") },
            { "singapurský", ("singapore", "Singapur") },
            { "hongkongská", ("hong-kong", "Hongkong") },
            { "hongkongský", ("hong-kong", "Hongkong") },
            { "indická", ("india", "Indie") },
            { "indický", ("india", "Indie") },
            { "íránská", ("iran", "Írán") },
            { "íránský", ("iran", "Írán") },
            { "saúdskoarabská", ("saudi-arabia", "Saúdská Arábie") },
            { "saúdskoarabský", ("saudi-arabia", "Saúdská Arábie") },
            { "saudskoarabská", ("saudi-arabia", "Saúdská Arábie") },
            { "saudskoarabský", ("saudi-arabia", "Saúdská Arábie") },
            { "emirátská", ("uae", "SAE") },
            { "emirátský", ("uae", "SAE") },
            { "katarská", ("qatar", "Katar") },
            { "katarský", ("qatar", "Katar") },
            { "ománská", ("oman", "Omán") },
            { "ománský", ("oman", "Omán") },
            { "kuvajstká", ("kuwait", "Kuvajt") },
            { "kuvajstký", ("kuwait", "Kuvajt") },
            { "bahrajnská", ("bahrain", "Bahrajn") },
            { "bahrajnský", ("bahrain", "Bahrajn") },
            { "uzbekistánská", ("uzbekistan", "Uzbekistán") },
            { "uzbekistánský", ("uzbekistan", "Uzbekistán") },
            { "bangladéšská", ("bangladesh", "Bangladéš") },
            { "bangladéšský", ("bangladesh", "Bangladéš") },
            { "kambodžská", ("cambodia", "Kambodža") },
            { "kambodžský", ("cambodia", "Kambodža") },
            { "libanonská", ("lebanon", "Libanon") },
            { "libanonský", ("lebanon", "Libanon") },
            { "jordánská", ("jordan", "Jordánsko") },
            { "jordánský", ("jordan", "Jordánsko") },
            { "irácká", ("iraq", "Irák") },
            { "irácký", ("iraq", "Irák") },
            { "syrská", ("syria", "Sýrie") },
            { "syrský", ("syria", "Sýrie") },

            // Africa
            { "egyptská", ("egypt", "Egypt") },
            { "egyptský", ("egypt", "Egypt") },
            { "marockká", ("morocco", "Maroko") },
            { "marocký", ("morocco", "Maroko") },
            { "tuniská", ("tunisia", "Tunisko") },
            { "tuniský", ("tunisia", "Tunisko") },
            { "alžírská", ("algeria", "Alžírsko") },
            { "alžírský", ("algeria", "Alžírsko") },
            { "jihoafrická", ("south-africa", "Jižní Afrika") },
            { "jihoafrický", ("south-africa", "Jižní Afrika") },
            { "nigerijská", ("nigeria", "Nigérie") },
            { "nigerijský", ("nigeria", "Nigérie") },
            { "ghanská", ("ghana", "Ghana") },
            { "ghanský", ("ghana", "Ghana") },
            { "keňská", ("kenya", "Keňa") },
            { "keňský", ("kenya", "Keňa") },
            { "tanzanská", ("tanzania", "Tanzanie") },
            { "tanzanský", ("tanzania", "Tanzanie") },
            { "ugandská", ("uganda", "Uganda") },
            { "ugandský", ("uganda", "Uganda") },
            { "etiopská", ("ethiopia", "Etiopie") },
            { "etiopský", ("ethiopia", "Etiopie") },
            { "rwandská", ("rwanda", "Rwanda") },
            { "rwandský", ("rwanda", "Rwanda") },
            { "malawijská", ("malawi", "Malawi") },
            { "malawijský", ("malawi", "Malawi") },
            { "malijská", ("mali", "Mali") },
            { "malijský", ("mali", "Mali") },
            { "mauritánská", ("mauritania", "Mauritánie") },
            { "mauritánský", ("mauritania", "Mauritánie") },

            // Czech & Slovakia (special handling)
            { "česká", ("czech-republic", "Česko") },
            { "český", ("czech-republic", "Česko") },
            { "slovenská", ("slovakia", "Slovensko") },
            { "slovenský", ("slovakia", "Slovensko") },
        };

        // Special cases that match full phrases
        // Note: Continental competitions (Liga mistrů, Copa Libertadores, etc.)
        // are intentionally NOT mapped - they will return null
        var specialCases = new Dictionary<string, (string Code, string Name)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Česká Chance Liga", ("czech-republic", "Česko") },
            { "Pobřeží slonoviny", ("ivory-coast", "Pobřeží slonoviny") },
            { "SAE", ("uae", "SAE") },  // Spojené arabské emiráty
            { "Maroko", ("morocco", "Maroko") },  // For "Africký pohár - Maroko"
        };

        // Check special cases first
        foreach (var (phrase, country) in specialCases)
        {
            if (title.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return (country.Code, country.Name);
            }
        }

        // Check country adjective mappings
        // IMPORTANT: Sort by key length descending to match longer/more specific patterns first
        // This prevents "irská" from matching before "severoirská" (which contains "irská")
        foreach (var (adjective, country) in countryMappings.OrderByDescending(kvp => kvp.Key.Length))
        {
            if (title.Contains(adjective, StringComparison.OrdinalIgnoreCase))
            {
                return (country.Code, country.Name);
            }
        }

        // Could not derive country
        return (null, null);
    }
}
