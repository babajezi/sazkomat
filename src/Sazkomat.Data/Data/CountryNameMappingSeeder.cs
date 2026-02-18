using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data;

/// <summary>
/// Seeds CountryNameMapping table with default mappings for providers.
/// These mappings are used to resolve country codes from Czech adjectives
/// in league names (e.g., "1. anglická liga" → england).
/// </summary>
public static class CountryNameMappingSeeder
{
    /// <summary>
    /// Seeds Tipsport country name mappings from the hardcoded dictionary.
    /// This includes ~240 Czech adjective → country code mappings.
    /// </summary>
    public static async Task SeedTipsportMappingsAsync(DataDbContext context)
    {
        const string providerCode = "tipsport";

        // Check if we already have mappings for this provider
        var existingCount = await context.CountryNameMappings
            .CountAsync(m => m.ProviderCode == providerCode);

        if (existingCount > 0)
        {
            // Already seeded - skip to avoid duplicates
            return;
        }

        var mappings = new List<CountryNameMapping>();

        // Priority levels:
        // 200 = special cases (checked first)
        // 100 = high priority (e.g., "severoirská" before "irská")
        // 50 = standard adjectives
        const int specialCasePriority = 200;
        const int highPriority = 100;
        const int standardPriority = 50;

        // ============================================
        // SPECIAL CASES (checked first, IsSpecialCase = true)
        // Only for valid country codes that exist in our database
        // Note: Continental competitions (Liga mistrů, Copa Libertadores, etc.)
        // are intentionally NOT mapped - they will return null and can be
        // handled separately or added to UnmatchedLeagues
        // ============================================
        AddSpecialCase(mappings, providerCode, "Česká Chance Liga", "czech-republic", "Česko", specialCasePriority);
        AddSpecialCase(mappings, providerCode, "Pobřeží slonoviny", "ivory-coast", "Pobřeží slonoviny", specialCasePriority);
        AddSpecialCase(mappings, providerCode, "SAE", "uae", "SAE", specialCasePriority, isCaseSensitive: true);
        AddSpecialCase(mappings, providerCode, "Maroko", "morocco", "Maroko", specialCasePriority);

        // ============================================
        // HIGH PRIORITY ADJECTIVES (must be checked before substrings)
        // e.g., "severoirská" before "irská"
        // ============================================
        AddAdjective(mappings, providerCode, "severoirská", "northern-ireland", "Severní Irsko", highPriority);
        AddAdjective(mappings, providerCode, "severoirský", "northern-ireland", "Severní Irsko", highPriority);

        // ============================================
        // EUROPEAN COUNTRIES
        // ============================================
        AddAdjective(mappings, providerCode, "anglická", "england", "Anglie", standardPriority);
        AddAdjective(mappings, providerCode, "anglický", "england", "Anglie", standardPriority);
        AddAdjective(mappings, providerCode, "německá", "germany", "Německo", standardPriority);
        AddAdjective(mappings, providerCode, "německý", "germany", "Německo", standardPriority);
        AddAdjective(mappings, providerCode, "španělská", "spain", "Španělsko", standardPriority);
        AddAdjective(mappings, providerCode, "španělský", "spain", "Španělsko", standardPriority);
        AddAdjective(mappings, providerCode, "italská", "italy", "Itálie", standardPriority);
        AddAdjective(mappings, providerCode, "italský", "italy", "Itálie", standardPriority);
        AddAdjective(mappings, providerCode, "francouzská", "france", "Francie", standardPriority);
        AddAdjective(mappings, providerCode, "francouzský", "france", "Francie", standardPriority);
        AddAdjective(mappings, providerCode, "nizozemská", "netherlands", "Nizozemsko", standardPriority);
        AddAdjective(mappings, providerCode, "nizozemský", "netherlands", "Nizozemsko", standardPriority);
        AddAdjective(mappings, providerCode, "belgická", "belgium", "Belgie", standardPriority);
        AddAdjective(mappings, providerCode, "belgický", "belgium", "Belgie", standardPriority);
        AddAdjective(mappings, providerCode, "portugalská", "portugal", "Portugalsko", standardPriority);
        AddAdjective(mappings, providerCode, "portugalský", "portugal", "Portugalsko", standardPriority);
        AddAdjective(mappings, providerCode, "rakouská", "austria", "Rakousko", standardPriority);
        AddAdjective(mappings, providerCode, "rakouský", "austria", "Rakousko", standardPriority);
        AddAdjective(mappings, providerCode, "švýcarská", "switzerland", "Švýcarsko", standardPriority);
        AddAdjective(mappings, providerCode, "švýcarský", "switzerland", "Švýcarsko", standardPriority);
        AddAdjective(mappings, providerCode, "polská", "poland", "Polsko", standardPriority);
        AddAdjective(mappings, providerCode, "polský", "poland", "Polsko", standardPriority);
        AddAdjective(mappings, providerCode, "řecká", "greece", "Řecko", standardPriority);
        AddAdjective(mappings, providerCode, "řecký", "greece", "Řecko", standardPriority);
        AddAdjective(mappings, providerCode, "turecká", "turkey", "Turecko", standardPriority);
        AddAdjective(mappings, providerCode, "turecký", "turkey", "Turecko", standardPriority);
        AddAdjective(mappings, providerCode, "ruská", "russia", "Rusko", standardPriority);
        AddAdjective(mappings, providerCode, "ruský", "russia", "Rusko", standardPriority);
        AddAdjective(mappings, providerCode, "ukrajinská", "ukraine", "Ukrajina", standardPriority);
        AddAdjective(mappings, providerCode, "ukrajinský", "ukraine", "Ukrajina", standardPriority);
        AddAdjective(mappings, providerCode, "skotská", "scotland", "Skotsko", standardPriority);
        AddAdjective(mappings, providerCode, "skotský", "scotland", "Skotsko", standardPriority);
        AddAdjective(mappings, providerCode, "velšská", "wales", "Wales", standardPriority);
        AddAdjective(mappings, providerCode, "velšský", "wales", "Wales", standardPriority);
        AddAdjective(mappings, providerCode, "dánská", "denmark", "Dánsko", standardPriority);
        AddAdjective(mappings, providerCode, "dánský", "denmark", "Dánsko", standardPriority);
        AddAdjective(mappings, providerCode, "norská", "norway", "Norsko", standardPriority);
        AddAdjective(mappings, providerCode, "norský", "norway", "Norsko", standardPriority);
        AddAdjective(mappings, providerCode, "švédská", "sweden", "Švédsko", standardPriority);
        AddAdjective(mappings, providerCode, "švédský", "sweden", "Švédsko", standardPriority);
        AddAdjective(mappings, providerCode, "finská", "finland", "Finsko", standardPriority);
        AddAdjective(mappings, providerCode, "finský", "finland", "Finsko", standardPriority);
        AddAdjective(mappings, providerCode, "chorvatská", "croatia", "Chorvatsko", standardPriority);
        AddAdjective(mappings, providerCode, "chorvatský", "croatia", "Chorvatsko", standardPriority);
        AddAdjective(mappings, providerCode, "srbská", "serbia", "Srbsko", standardPriority);
        AddAdjective(mappings, providerCode, "srbský", "serbia", "Srbsko", standardPriority);
        AddAdjective(mappings, providerCode, "maďarská", "hungary", "Maďarsko", standardPriority);
        AddAdjective(mappings, providerCode, "maďarský", "hungary", "Maďarsko", standardPriority);
        AddAdjective(mappings, providerCode, "rumunská", "romania", "Rumunsko", standardPriority);
        AddAdjective(mappings, providerCode, "rumunský", "romania", "Rumunsko", standardPriority);
        AddAdjective(mappings, providerCode, "bulharská", "bulgaria", "Bulharsko", standardPriority);
        AddAdjective(mappings, providerCode, "bulharský", "bulgaria", "Bulharsko", standardPriority);
        AddAdjective(mappings, providerCode, "kyperská", "cyprus", "Kypr", standardPriority);
        AddAdjective(mappings, providerCode, "kyperský", "cyprus", "Kypr", standardPriority);
        AddAdjective(mappings, providerCode, "izraelská", "israel", "Izrael", standardPriority);
        AddAdjective(mappings, providerCode, "izraelský", "israel", "Izrael", standardPriority);
        AddAdjective(mappings, providerCode, "irská", "ireland", "Irsko", standardPriority);
        AddAdjective(mappings, providerCode, "irský", "ireland", "Irsko", standardPriority);
        AddAdjective(mappings, providerCode, "islandská", "iceland", "Island", standardPriority);
        AddAdjective(mappings, providerCode, "islandský", "iceland", "Island", standardPriority);
        AddAdjective(mappings, providerCode, "slovinská", "slovenia", "Slovinsko", standardPriority);
        AddAdjective(mappings, providerCode, "slovinský", "slovenia", "Slovinsko", standardPriority);
        AddAdjective(mappings, providerCode, "bosenská", "bosnia-herzegovina", "Bosna a Hercegovina", standardPriority);
        AddAdjective(mappings, providerCode, "bosenský", "bosnia-herzegovina", "Bosna a Hercegovina", standardPriority);
        AddAdjective(mappings, providerCode, "černohorská", "montenegro", "Černá Hora", standardPriority);
        AddAdjective(mappings, providerCode, "černohorský", "montenegro", "Černá Hora", standardPriority);
        AddAdjective(mappings, providerCode, "makedonská", "north-macedonia", "Severní Makedonie", standardPriority);
        AddAdjective(mappings, providerCode, "makedonský", "north-macedonia", "Severní Makedonie", standardPriority);
        AddAdjective(mappings, providerCode, "albánská", "albania", "Albánie", standardPriority);
        AddAdjective(mappings, providerCode, "albánský", "albania", "Albánie", standardPriority);
        AddAdjective(mappings, providerCode, "kosovská", "kosovo", "Kosovo", standardPriority);
        AddAdjective(mappings, providerCode, "kosovský", "kosovo", "Kosovo", standardPriority);
        AddAdjective(mappings, providerCode, "gruzínská", "georgia", "Gruzie", standardPriority);
        AddAdjective(mappings, providerCode, "gruzínský", "georgia", "Gruzie", standardPriority);
        AddAdjective(mappings, providerCode, "ázerbájdžánská", "azerbaijan", "Ázerbájdžán", standardPriority);
        AddAdjective(mappings, providerCode, "ázerbájdžánský", "azerbaijan", "Ázerbájdžán", standardPriority);
        AddAdjective(mappings, providerCode, "arménská", "armenia", "Arménie", standardPriority);
        AddAdjective(mappings, providerCode, "arménský", "armenia", "Arménie", standardPriority);
        AddAdjective(mappings, providerCode, "kazašská", "kazakhstan", "Kazachstán", standardPriority);
        AddAdjective(mappings, providerCode, "kazašský", "kazakhstan", "Kazachstán", standardPriority);
        AddAdjective(mappings, providerCode, "běloruská", "belarus", "Bělorusko", standardPriority);
        AddAdjective(mappings, providerCode, "běloruský", "belarus", "Bělorusko", standardPriority);
        AddAdjective(mappings, providerCode, "litevská", "lithuania", "Litva", standardPriority);
        AddAdjective(mappings, providerCode, "litevský", "lithuania", "Litva", standardPriority);
        AddAdjective(mappings, providerCode, "lotyšská", "latvia", "Lotyšsko", standardPriority);
        AddAdjective(mappings, providerCode, "lotyšský", "latvia", "Lotyšsko", standardPriority);
        AddAdjective(mappings, providerCode, "estonská", "estonia", "Estonsko", standardPriority);
        AddAdjective(mappings, providerCode, "estonský", "estonia", "Estonsko", standardPriority);
        AddAdjective(mappings, providerCode, "maltská", "malta", "Malta", standardPriority);
        AddAdjective(mappings, providerCode, "maltský", "malta", "Malta", standardPriority);
        AddAdjective(mappings, providerCode, "lucemburská", "luxembourg", "Lucembursko", standardPriority);
        AddAdjective(mappings, providerCode, "lucemburský", "luxembourg", "Lucembursko", standardPriority);

        // ============================================
        // AMERICAS
        // ============================================
        AddAdjective(mappings, providerCode, "americká", "usa", "USA", standardPriority);
        AddAdjective(mappings, providerCode, "americký", "usa", "USA", standardPriority);
        AddAdjective(mappings, providerCode, "argentinská", "argentina", "Argentina", standardPriority);
        AddAdjective(mappings, providerCode, "argentinský", "argentina", "Argentina", standardPriority);
        AddAdjective(mappings, providerCode, "brazilská", "brazil", "Brazílie", standardPriority);
        AddAdjective(mappings, providerCode, "brazilský", "brazil", "Brazílie", standardPriority);
        AddAdjective(mappings, providerCode, "mexická", "mexico", "Mexiko", standardPriority);
        AddAdjective(mappings, providerCode, "mexický", "mexico", "Mexiko", standardPriority);
        AddAdjective(mappings, providerCode, "chilská", "chile", "Chile", standardPriority);
        AddAdjective(mappings, providerCode, "chilský", "chile", "Chile", standardPriority);
        AddAdjective(mappings, providerCode, "kolumbijská", "colombia", "Kolumbie", standardPriority);
        AddAdjective(mappings, providerCode, "kolumbijský", "colombia", "Kolumbie", standardPriority);
        AddAdjective(mappings, providerCode, "peruánská", "peru", "Peru", standardPriority);
        AddAdjective(mappings, providerCode, "peruánský", "peru", "Peru", standardPriority);
        AddAdjective(mappings, providerCode, "uruguayská", "uruguay", "Uruguay", standardPriority);
        AddAdjective(mappings, providerCode, "uruguayský", "uruguay", "Uruguay", standardPriority);
        AddAdjective(mappings, providerCode, "paraguayská", "paraguay", "Paraguay", standardPriority);
        AddAdjective(mappings, providerCode, "paraguayský", "paraguay", "Paraguay", standardPriority);
        AddAdjective(mappings, providerCode, "ekvádorská", "ecuador", "Ekvádor", standardPriority);
        AddAdjective(mappings, providerCode, "ekvádorský", "ecuador", "Ekvádor", standardPriority);
        AddAdjective(mappings, providerCode, "bolívijská", "bolivia", "Bolívie", standardPriority);
        AddAdjective(mappings, providerCode, "bolívijský", "bolivia", "Bolívie", standardPriority);
        AddAdjective(mappings, providerCode, "venezuelská", "venezuela", "Venezuela", standardPriority);
        AddAdjective(mappings, providerCode, "venezuelský", "venezuela", "Venezuela", standardPriority);
        AddAdjective(mappings, providerCode, "kostarická", "costa-rica", "Kostarika", standardPriority);
        AddAdjective(mappings, providerCode, "kostarický", "costa-rica", "Kostarika", standardPriority);
        AddAdjective(mappings, providerCode, "honduraská", "honduras", "Honduras", standardPriority);
        AddAdjective(mappings, providerCode, "honduraský", "honduras", "Honduras", standardPriority);
        AddAdjective(mappings, providerCode, "guatemalská", "guatemala", "Guatemala", standardPriority);
        AddAdjective(mappings, providerCode, "guatemalský", "guatemala", "Guatemala", standardPriority);
        AddAdjective(mappings, providerCode, "salvadorská", "el-salvador", "Salvador", standardPriority);
        AddAdjective(mappings, providerCode, "salvadorský", "el-salvador", "Salvador", standardPriority);
        AddAdjective(mappings, providerCode, "panamská", "panama", "Panama", standardPriority);
        AddAdjective(mappings, providerCode, "panamský", "panama", "Panama", standardPriority);
        AddAdjective(mappings, providerCode, "jamajská", "jamaica", "Jamajka", standardPriority);
        AddAdjective(mappings, providerCode, "jamajský", "jamaica", "Jamajka", standardPriority);
        AddAdjective(mappings, providerCode, "nikaragujská", "nicaragua", "Nikaragua", standardPriority);
        AddAdjective(mappings, providerCode, "nikaragujský", "nicaragua", "Nikaragua", standardPriority);
        AddAdjective(mappings, providerCode, "dominikánská", "dominican-republic", "Dominikánská republika", standardPriority);
        AddAdjective(mappings, providerCode, "dominikánský", "dominican-republic", "Dominikánská republika", standardPriority);
        AddAdjective(mappings, providerCode, "kanadská", "canada", "Kanada", standardPriority);
        AddAdjective(mappings, providerCode, "kanadský", "canada", "Kanada", standardPriority);

        // ============================================
        // ASIA & OCEANIA
        // ============================================
        AddAdjective(mappings, providerCode, "japonská", "japan", "Japonsko", standardPriority);
        AddAdjective(mappings, providerCode, "japonský", "japan", "Japonsko", standardPriority);
        AddAdjective(mappings, providerCode, "korejská", "south-korea", "Jižní Korea", standardPriority);
        AddAdjective(mappings, providerCode, "korejský", "south-korea", "Jižní Korea", standardPriority);
        AddAdjective(mappings, providerCode, "čínská", "china", "Čína", standardPriority);
        AddAdjective(mappings, providerCode, "čínský", "china", "Čína", standardPriority);
        AddAdjective(mappings, providerCode, "australská", "australia", "Austrálie", standardPriority);
        AddAdjective(mappings, providerCode, "australský", "australia", "Austrálie", standardPriority);
        AddAdjective(mappings, providerCode, "novozélandská", "new-zealand", "Nový Zéland", standardPriority);
        AddAdjective(mappings, providerCode, "novozélandský", "new-zealand", "Nový Zéland", standardPriority);
        AddAdjective(mappings, providerCode, "thajská", "thailand", "Thajsko", standardPriority);
        AddAdjective(mappings, providerCode, "thajský", "thailand", "Thajsko", standardPriority);
        AddAdjective(mappings, providerCode, "vietnamská", "vietnam", "Vietnam", standardPriority);
        AddAdjective(mappings, providerCode, "vietnamský", "vietnam", "Vietnam", standardPriority);
        AddAdjective(mappings, providerCode, "indonéská", "indonesia", "Indonésie", standardPriority);
        AddAdjective(mappings, providerCode, "indonéský", "indonesia", "Indonésie", standardPriority);
        AddAdjective(mappings, providerCode, "malajská", "malaysia", "Malajsie", standardPriority);
        AddAdjective(mappings, providerCode, "malajský", "malaysia", "Malajsie", standardPriority);
        AddAdjective(mappings, providerCode, "malajsijská", "malaysia", "Malajsie", standardPriority);
        AddAdjective(mappings, providerCode, "malajsijský", "malaysia", "Malajsie", standardPriority);
        AddAdjective(mappings, providerCode, "singapurská", "singapore", "Singapur", standardPriority);
        AddAdjective(mappings, providerCode, "singapurský", "singapore", "Singapur", standardPriority);
        AddAdjective(mappings, providerCode, "hongkongská", "hong-kong", "Hongkong", standardPriority);
        AddAdjective(mappings, providerCode, "hongkongský", "hong-kong", "Hongkong", standardPriority);
        AddAdjective(mappings, providerCode, "indická", "india", "Indie", standardPriority);
        AddAdjective(mappings, providerCode, "indický", "india", "Indie", standardPriority);
        AddAdjective(mappings, providerCode, "íránská", "iran", "Írán", standardPriority);
        AddAdjective(mappings, providerCode, "íránský", "iran", "Írán", standardPriority);
        AddAdjective(mappings, providerCode, "saúdskoarabská", "saudi-arabia", "Saúdská Arábie", standardPriority);
        AddAdjective(mappings, providerCode, "saúdskoarabský", "saudi-arabia", "Saúdská Arábie", standardPriority);
        AddAdjective(mappings, providerCode, "saudskoarabská", "saudi-arabia", "Saúdská Arábie", standardPriority);
        AddAdjective(mappings, providerCode, "saudskoarabský", "saudi-arabia", "Saúdská Arábie", standardPriority);
        AddAdjective(mappings, providerCode, "emirátská", "uae", "SAE", standardPriority);
        AddAdjective(mappings, providerCode, "emirátský", "uae", "SAE", standardPriority);
        AddAdjective(mappings, providerCode, "katarská", "qatar", "Katar", standardPriority);
        AddAdjective(mappings, providerCode, "katarský", "qatar", "Katar", standardPriority);
        AddAdjective(mappings, providerCode, "ománská", "oman", "Omán", standardPriority);
        AddAdjective(mappings, providerCode, "ománský", "oman", "Omán", standardPriority);
        AddAdjective(mappings, providerCode, "kuvajstká", "kuwait", "Kuvajt", standardPriority);
        AddAdjective(mappings, providerCode, "kuvajstký", "kuwait", "Kuvajt", standardPriority);
        AddAdjective(mappings, providerCode, "bahrajnská", "bahrain", "Bahrajn", standardPriority);
        AddAdjective(mappings, providerCode, "bahrajnský", "bahrain", "Bahrajn", standardPriority);
        AddAdjective(mappings, providerCode, "uzbekistánská", "uzbekistan", "Uzbekistán", standardPriority);
        AddAdjective(mappings, providerCode, "uzbekistánský", "uzbekistan", "Uzbekistán", standardPriority);
        AddAdjective(mappings, providerCode, "bangladéšská", "bangladesh", "Bangladéš", standardPriority);
        AddAdjective(mappings, providerCode, "bangladéšský", "bangladesh", "Bangladéš", standardPriority);
        AddAdjective(mappings, providerCode, "kambodžská", "cambodia", "Kambodža", standardPriority);
        AddAdjective(mappings, providerCode, "kambodžský", "cambodia", "Kambodža", standardPriority);
        AddAdjective(mappings, providerCode, "libanonská", "lebanon", "Libanon", standardPriority);
        AddAdjective(mappings, providerCode, "libanonský", "lebanon", "Libanon", standardPriority);
        AddAdjective(mappings, providerCode, "jordánská", "jordan", "Jordánsko", standardPriority);
        AddAdjective(mappings, providerCode, "jordánský", "jordan", "Jordánsko", standardPriority);
        AddAdjective(mappings, providerCode, "irácká", "iraq", "Irák", standardPriority);
        AddAdjective(mappings, providerCode, "irácký", "iraq", "Irák", standardPriority);
        AddAdjective(mappings, providerCode, "syrská", "syria", "Sýrie", standardPriority);
        AddAdjective(mappings, providerCode, "syrský", "syria", "Sýrie", standardPriority);

        // ============================================
        // AFRICA
        // ============================================
        AddAdjective(mappings, providerCode, "egyptská", "egypt", "Egypt", standardPriority);
        AddAdjective(mappings, providerCode, "egyptský", "egypt", "Egypt", standardPriority);
        AddAdjective(mappings, providerCode, "marockká", "morocco", "Maroko", standardPriority);
        AddAdjective(mappings, providerCode, "marocký", "morocco", "Maroko", standardPriority);
        AddAdjective(mappings, providerCode, "tuniská", "tunisia", "Tunisko", standardPriority);
        AddAdjective(mappings, providerCode, "tuniský", "tunisia", "Tunisko", standardPriority);
        AddAdjective(mappings, providerCode, "alžírská", "algeria", "Alžírsko", standardPriority);
        AddAdjective(mappings, providerCode, "alžírský", "algeria", "Alžírsko", standardPriority);
        AddAdjective(mappings, providerCode, "jihoafrická", "south-africa", "Jižní Afrika", standardPriority);
        AddAdjective(mappings, providerCode, "jihoafrický", "south-africa", "Jižní Afrika", standardPriority);
        AddAdjective(mappings, providerCode, "nigerijská", "nigeria", "Nigérie", standardPriority);
        AddAdjective(mappings, providerCode, "nigerijský", "nigeria", "Nigérie", standardPriority);
        AddAdjective(mappings, providerCode, "ghanská", "ghana", "Ghana", standardPriority);
        AddAdjective(mappings, providerCode, "ghanský", "ghana", "Ghana", standardPriority);
        AddAdjective(mappings, providerCode, "keňská", "kenya", "Keňa", standardPriority);
        AddAdjective(mappings, providerCode, "keňský", "kenya", "Keňa", standardPriority);
        AddAdjective(mappings, providerCode, "tanzanská", "tanzania", "Tanzanie", standardPriority);
        AddAdjective(mappings, providerCode, "tanzanský", "tanzania", "Tanzanie", standardPriority);
        AddAdjective(mappings, providerCode, "ugandská", "uganda", "Uganda", standardPriority);
        AddAdjective(mappings, providerCode, "ugandský", "uganda", "Uganda", standardPriority);
        AddAdjective(mappings, providerCode, "etiopská", "ethiopia", "Etiopie", standardPriority);
        AddAdjective(mappings, providerCode, "etiopský", "ethiopia", "Etiopie", standardPriority);
        AddAdjective(mappings, providerCode, "rwandská", "rwanda", "Rwanda", standardPriority);
        AddAdjective(mappings, providerCode, "rwandský", "rwanda", "Rwanda", standardPriority);
        AddAdjective(mappings, providerCode, "malawijská", "malawi", "Malawi", standardPriority);
        AddAdjective(mappings, providerCode, "malawijský", "malawi", "Malawi", standardPriority);
        AddAdjective(mappings, providerCode, "malijská", "mali", "Mali", standardPriority);
        AddAdjective(mappings, providerCode, "malijský", "mali", "Mali", standardPriority);
        AddAdjective(mappings, providerCode, "mauritánská", "mauritania", "Mauritánie", standardPriority);
        AddAdjective(mappings, providerCode, "mauritánský", "mauritania", "Mauritánie", standardPriority);

        // ============================================
        // CZECH & SLOVAKIA
        // ============================================
        AddAdjective(mappings, providerCode, "česká", "czech-republic", "Česko", standardPriority);
        AddAdjective(mappings, providerCode, "český", "czech-republic", "Česko", standardPriority);
        AddAdjective(mappings, providerCode, "slovenská", "slovakia", "Slovensko", standardPriority);
        AddAdjective(mappings, providerCode, "slovenský", "slovakia", "Slovensko", standardPriority);

        // Bulk insert all mappings
        await context.CountryNameMappings.AddRangeAsync(mappings);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper to add a standard adjective mapping (substring match)
    /// </summary>
    private static void AddAdjective(
        List<CountryNameMapping> mappings,
        string providerCode,
        string pattern,
        string countryCode,
        string localizedName,
        int priority)
    {
        mappings.Add(new CountryNameMapping
        {
            ProviderCode = providerCode,
            ProviderCountryName = pattern,
            BetExplorerCode = countryCode,
            LocalizedName = localizedName,
            MatchType = "substring",
            IsCaseSensitive = false,
            IsSpecialCase = false,
            Priority = priority,
            IsActive = true
        });
    }

    /// <summary>
    /// Helper to add a special case mapping (full phrase or acronym)
    /// </summary>
    private static void AddSpecialCase(
        List<CountryNameMapping> mappings,
        string providerCode,
        string pattern,
        string countryCode,
        string localizedName,
        int priority,
        bool isCaseSensitive = false)
    {
        mappings.Add(new CountryNameMapping
        {
            ProviderCode = providerCode,
            ProviderCountryName = pattern,
            BetExplorerCode = countryCode,
            LocalizedName = localizedName,
            MatchType = "substring", // Special cases also use substring matching
            IsCaseSensitive = isCaseSensitive,
            IsSpecialCase = true,
            Priority = priority,
            IsActive = true
        });
    }
}
