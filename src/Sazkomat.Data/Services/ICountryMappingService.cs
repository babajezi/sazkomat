namespace Sazkomat.Data.Services;

/// <summary>
/// Service for resolving country codes from text patterns.
/// Used by scrapers (Tipsport, Chance, etc.) to map provider-specific country names
/// to standardized BetExplorer country codes.
/// </summary>
public interface ICountryMappingService
{
    /// <summary>
    /// Resolves a country code and localized name from input text using pattern matching.
    /// </summary>
    /// <param name="providerCode">The provider code (e.g., "tipsport", "chance")</param>
    /// <param name="inputText">The input text to search for country patterns (e.g., "1. anglická liga")</param>
    /// <returns>
    /// A tuple with (CountryCode, LocalizedName) if a match is found,
    /// or (null, null) if no match is found.
    /// </returns>
    Task<(string? CountryCode, string? LocalizedName)> ResolveCountryAsync(string providerCode, string inputText);
}
