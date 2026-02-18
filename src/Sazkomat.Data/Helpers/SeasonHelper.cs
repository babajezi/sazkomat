namespace Sazkomat.Data.Helpers;

public static class SeasonHelper
{
    /// <summary>
    /// Vypočítá předchozí sezónu z aktuální sezóny
    /// </summary>
    /// <param name="currentSeason">Aktuální sezóna (např. "2024-2025" nebo "2024")</param>
    /// <returns>Předchozí sezóna (např. "2023-2024" nebo "2023")</returns>
    /// <exception cref="ArgumentException">Pokud formát sezóny není validní</exception>
    public static string GetPreviousSeasonPattern(string currentSeason)
    {
        if (string.IsNullOrWhiteSpace(currentSeason))
        {
            throw new ArgumentException("Season cannot be null or empty", nameof(currentSeason));
        }

        // Split by '-' or '/' delimiter
        var parts = currentSeason.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
        {
            // Two-year format: "2024-2025" → "2023-2024"
            return $"{startYear - 1}-{endYear - 1}";
        }
        else if (parts.Length == 1 && int.TryParse(parts[0], out int year))
        {
            // Single-year format: "2024" → "2023"
            return $"{year - 1}";
        }

        throw new ArgumentException($"Invalid season format: {currentSeason}. Expected format: 'YYYY-YYYY' or 'YYYY'", nameof(currentSeason));
    }

    /// <summary>
    /// Parsuje sezónu na (startYear, endYear) tuple
    /// </summary>
    /// <param name="season">Sezóna (např. "2024-2025" nebo "2024")</param>
    /// <returns>Tuple (startYear, endYear). Pro jedno-letou sezónu je endYear null</returns>
    public static (int startYear, int? endYear) ParseSeason(string season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            throw new ArgumentException("Season cannot be null or empty", nameof(season));
        }

        var parts = season.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 && int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
        {
            return (startYear, endYear);
        }
        else if (parts.Length == 1 && int.TryParse(parts[0], out int year))
        {
            return (year, null);
        }

        throw new ArgumentException($"Invalid season format: {season}. Expected format: 'YYYY-YYYY' or 'YYYY'", nameof(season));
    }
}
