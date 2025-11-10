namespace Sazkomat.DataImport.Helpers;

public static class CountryHelper
{
    /// <summary>
    /// Converts ISO country code to flag emoji
    /// Uses Regional Indicator Symbols (U+1F1E6-U+1F1FF)
    /// </summary>
    public static string GetFlagEmoji(string isoCode)
    {
        if (string.IsNullOrEmpty(isoCode) || isoCode.Length < 2)
            return "";

        // Convert to uppercase and take first 2 characters
        var code = isoCode.ToUpperInvariant().Substring(0, 2);

        // Validate that both characters are A-Z
        if (!char.IsLetter(code[0]) || !char.IsLetter(code[1]))
            return "";

        // Convert each letter to Regional Indicator Symbol
        // A = U+1F1E6, B = U+1F1E7, ..., Z = U+1F1FF
        var first = char.ConvertFromUtf32(0x1F1E6 + (code[0] - 'A'));
        var second = char.ConvertFromUtf32(0x1F1E6 + (code[1] - 'A'));

        return first + second;
    }

    /// <summary>
    /// Maps BetExplorer country codes to ISO 3166-1 alpha-2 codes
    /// </summary>
    public static string GetIsoCountryCode(string betExplorerCode)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Western Europe
            { "england", "GB" },
            { "scotland", "GB" },
            { "wales", "GB" },
            { "northern-ireland", "GB" },
            { "netherlands", "NL" },
            { "switzerland", "CH" },
            { "belgium", "BE" },
            { "portugal", "PT" },
            { "spain", "ES" },
            { "italy", "IT" },
            { "france", "FR" },
            { "germany", "DE" },
            { "austria", "AT" },
            { "luxembourg", "LU" },
            { "ireland", "IE" },
            { "malta", "MT" },

            // Nordic
            { "sweden", "SE" },
            { "denmark", "DK" },
            { "norway", "NO" },
            { "finland", "FI" },
            { "iceland", "IS" },

            // Eastern Europe
            { "poland", "PL" },
            { "czech-republic", "CZ" },
            { "slovakia", "SK" },
            { "hungary", "HU" },
            { "romania", "RO" },
            { "bulgaria", "BG" },
            { "slovenia", "SI" },
            { "croatia", "HR" },
            { "serbia", "RS" },
            { "bosnia-herzegovina", "BA" },
            { "north-macedonia", "MK" },
            { "albania", "AL" },
            { "montenegro", "ME" },
            { "kosovo", "XK" },
            { "lithuania", "LT" },
            { "latvia", "LV" },
            { "estonia", "EE" },
            { "moldova", "MD" },
            { "belarus", "BY" },

            // CIS & Asia
            { "russia", "RU" },
            { "ukraine", "UA" },
            { "turkey", "TR" },
            { "georgia", "GE" },
            { "armenia", "AM" },
            { "azerbaijan", "AZ" },
            { "kazakhstan", "KZ" },
            { "china", "CN" },
            { "japan", "JP" },
            { "south-korea", "KR" },
            { "india", "IN" },
            { "thailand", "TH" },
            { "vietnam", "VN" },
            { "indonesia", "ID" },
            { "malaysia", "MY" },
            { "singapore", "SG" },
            { "philippines", "PH" },
            { "australia", "AU" },
            { "new-zealand", "NZ" },

            // Middle East & Africa
            { "israel", "IL" },
            { "saudi-arabia", "SA" },
            { "uae", "AE" },
            { "qatar", "QA" },
            { "egypt", "EG" },
            { "south-africa", "ZA" },
            { "morocco", "MA" },
            { "tunisia", "TN" },
            { "algeria", "DZ" },
            { "nigeria", "NG" },
            { "ghana", "GH" },
            { "kenya", "KE" },
            { "cyprus", "CY" },

            // Americas
            { "usa", "US" },
            { "canada", "CA" },
            { "mexico", "MX" },
            { "argentina", "AR" },
            { "brazil", "BR" },
            { "chile", "CL" },
            { "colombia", "CO" },
            { "uruguay", "UY" },
            { "paraguay", "PY" },
            { "peru", "PE" },
            { "ecuador", "EC" },
            { "bolivia", "BO" },
            { "venezuela", "VE" },

            // Balkans
            { "greece", "GR" },

            // Additional European countries
            { "andorra", "AD" },
            { "liechtenstein", "LI" },
            { "faroe-islands", "FO" },
            { "gibraltar", "GI" },
            { "san-marino", "SM" },
            { "bosnia-and-herzegovina", "BA" },

            // Additional African countries
            { "angola", "AO" },
            { "benin", "BJ" },
            { "botswana", "BW" },
            { "burkina-faso", "BF" },
            { "burundi", "BI" },
            { "cameroon", "CM" },
            { "cape-verde", "CV" },
            { "chad", "TD" },
            { "djibouti", "DJ" },
            { "dr-congo", "CD" },
            { "republic-of-the-congo", "CG" },
            { "eswatini", "SZ" },
            { "ethiopia", "ET" },
            { "gabon", "GA" },
            { "gambia", "GM" },
            { "guinea", "GN" },
            { "ivory-coast", "CI" },
            { "lesotho", "LS" },
            { "liberia", "LR" },
            { "libya", "LY" },
            { "madagascar", "MG" },
            { "malawi", "MW" },
            { "mali", "ML" },
            { "mauritania", "MR" },
            { "mauritius", "MU" },
            { "mozambique", "MZ" },
            { "namibia", "NA" },
            { "niger", "NE" },
            { "rwanda", "RW" },
            { "sao-tome-and-principe", "ST" },
            { "senegal", "SN" },
            { "seychelles", "SC" },
            { "sierra-leone", "SL" },
            { "somalia", "SO" },
            { "sudan", "SD" },
            { "tanzania", "TZ" },
            { "togo", "TG" },
            { "uganda", "UG" },
            { "zambia", "ZM" },
            { "zimbabwe", "ZW" },
            { "reunion", "RE" },
            { "martinique", "MQ" },

            // Additional Middle East countries
            { "bahrain", "BH" },
            { "iran", "IR" },
            { "iraq", "IQ" },
            { "jordan", "JO" },
            { "kuwait", "KW" },
            { "lebanon", "LB" },
            { "oman", "OM" },
            { "palestine", "PS" },
            { "syria", "SY" },
            { "yemen", "YE" },
            { "united-arab-emirates", "AE" },

            // Additional Asian countries
            { "bangladesh", "BD" },
            { "bhutan", "BT" },
            { "cambodia", "KH" },
            { "hong-kong", "HK" },
            { "kyrgyzstan", "KG" },
            { "laos", "LA" },
            { "macao", "MO" },
            { "maldives", "MV" },
            { "mongolia", "MN" },
            { "myanmar", "MM" },
            { "pakistan", "PK" },
            { "sri-lanka", "LK" },
            { "taiwan", "TW" },
            { "tajikistan", "TJ" },
            { "turkmenistan", "TM" },
            { "uzbekistan", "UZ" },

            // Additional Americas countries
            { "antigua-barbuda", "AG" },
            { "aruba", "AW" },
            { "barbados", "BB" },
            { "bermuda", "BM" },
            { "costa-rica", "CR" },
            { "cuba", "CU" },
            { "curacao", "CW" },
            { "dominican-republic", "DO" },
            { "el-salvador", "SV" },
            { "guatemala", "GT" },
            { "haiti", "HT" },
            { "honduras", "HN" },
            { "jamaica", "JM" },
            { "nicaragua", "NI" },
            { "panama", "PA" },
            { "trinidad-and-tobago", "TT" },
            { "grenada", "GD" },
            { "suriname", "SR" },

            // Additional Oceania countries
            { "fiji", "FJ" },

            // Historical/Special regions
            { "serbia-and-montenegro", "CS" },
        };

        if (mapping.TryGetValue(betExplorerCode, out var isoCode))
        {
            return isoCode;
        }

        // Fallback: return empty if not found
        return "";
    }
}
