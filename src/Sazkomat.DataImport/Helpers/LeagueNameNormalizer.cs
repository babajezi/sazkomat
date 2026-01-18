using System.Text.RegularExpressions;

namespace Sazkomat.DataImport.Helpers;

/// <summary>
/// Helper for normalizing league names for comparison.
/// Handles differences like extra whitespace, casing, etc.
/// </summary>
public static partial class LeagueNameNormalizer
{
    /// <summary>
    /// Normalizes a league name for consistent comparison.
    /// - Trims whitespace
    /// - Collapses multiple spaces to single space
    /// - Converts to lowercase
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Trim, collapse whitespace, lowercase
        var normalized = WhitespaceRegex().Replace(name.Trim(), " ");
        return normalized.ToLowerInvariant();
    }

    /// <summary>
    /// Checks if two league names are equivalent after normalization.
    /// </summary>
    public static bool AreEquivalent(string? a, string? b)
    {
        return Normalize(a) == Normalize(b);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
