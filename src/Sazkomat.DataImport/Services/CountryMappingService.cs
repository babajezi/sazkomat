using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for resolving country codes from text patterns.
/// Uses the CountryNameMapping table to find matches.
/// </summary>
public class CountryMappingService : ICountryMappingService
{
    private readonly ICountryNameMappingRepository _mappingRepository;
    private readonly ILogger<CountryMappingService> _logger;

    public CountryMappingService(
        ICountryNameMappingRepository mappingRepository,
        ILogger<CountryMappingService> logger)
    {
        _mappingRepository = mappingRepository;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a country code and localized name from input text using pattern matching.
    /// </summary>
    public async Task<(string? CountryCode, string? LocalizedName)> ResolveCountryAsync(
        string providerCode,
        string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return (null, null);
        }

        var mapping = await _mappingRepository.FindByPatternAsync(providerCode, inputText);

        if (mapping != null)
        {
            _logger.LogDebug(
                "Resolved country from '{InputText}': {CountryCode} ({LocalizedName}) [provider: {Provider}]",
                inputText, mapping.BetExplorerCode, mapping.LocalizedName ?? mapping.BetExplorerCode, providerCode);

            return (mapping.BetExplorerCode, mapping.LocalizedName);
        }

        _logger.LogDebug(
            "No country mapping found for '{InputText}' [provider: {Provider}]",
            inputText, providerCode);

        return (null, null);
    }
}
