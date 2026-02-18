using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public class CountryNameMappingRepository : ICountryNameMappingRepository
{
    private readonly DataDbContext _context;

    public CountryNameMappingRepository(DataDbContext context)
    {
        _context = context;
    }

    public async Task<CountryNameMapping?> GetByIdAsync(Guid id)
    {
        return await _context.CountryNameMappings.FindAsync(id);
    }

    public async Task<List<CountryNameMapping>> GetAllAsync()
    {
        return await _context.CountryNameMappings
            .OrderBy(m => m.ProviderCode)
            .ThenBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<List<CountryNameMapping>> GetActiveByProviderAsync(string providerCode)
    {
        return await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode && m.IsActive)
            .OrderBy(m => m.Priority)
            .ToListAsync();
    }

    public async Task<CountryNameMapping?> FindMappingAsync(
        string providerCode,
        string providerCountryName)
    {
        return await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode
                     && m.ProviderCountryName == providerCountryName
                     && m.IsActive)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Find any mapping for the given provider and country name, including inactive ones.
    /// Used for checking if a mapping exists before creating a new one.
    /// </summary>
    public async Task<CountryNameMapping?> FindAnyMappingAsync(
        string providerCode,
        string providerCountryName)
    {
        return await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode
                     && m.ProviderCountryName == providerCountryName)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();
    }

    public async Task<CountryNameMapping> CreateAsync(CountryNameMapping mapping)
    {
        _context.CountryNameMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<CountryNameMapping> UpdateAsync(CountryNameMapping mapping)
    {
        mapping.UpdatedAt = DateTime.UtcNow;
        _context.CountryNameMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteAsync(Guid id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            _context.CountryNameMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task TrackUsageAsync(Guid mappingId, Guid providerCountryId)
    {
        var mapping = await GetByIdAsync(mappingId);
        if (mapping != null)
        {
            mapping.LastUsedAt = DateTime.UtcNow;
            mapping.UsageCount++;
            mapping.LastProviderCountryId = providerCountryId;
            mapping.UpdatedAt = DateTime.UtcNow;

            _context.CountryNameMappings.Update(mapping);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Find a mapping by pattern matching the input text against all active mappings.
    /// Special cases are checked first (with highest priority), then regular mappings.
    /// Priority ordering: higher priority number = checked first.
    /// </summary>
    public async Task<CountryNameMapping?> FindByPatternAsync(string providerCode, string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            return null;

        // Get all active mappings for this provider, ordered by priority (descending - higher = first)
        var mappings = await _context.CountryNameMappings
            .Where(m => m.ProviderCode == providerCode && m.IsActive)
            .OrderByDescending(m => m.Priority)
            .ToListAsync();

        // 1. Check special cases first (they have highest priority)
        foreach (var mapping in mappings.Where(m => m.IsSpecialCase))
        {
            if (MatchesPattern(mapping, inputText))
                return mapping;
        }

        // 2. Check regular mappings by priority
        foreach (var mapping in mappings.Where(m => !m.IsSpecialCase))
        {
            if (MatchesPattern(mapping, inputText))
                return mapping;
        }

        return null;
    }

    /// <summary>
    /// Check if the input text matches the mapping pattern based on MatchType
    /// </summary>
    private static bool MatchesPattern(CountryNameMapping mapping, string inputText)
    {
        var comparison = mapping.IsCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return mapping.MatchType?.ToLowerInvariant() switch
        {
            "exact" => inputText.Equals(mapping.ProviderCountryName, comparison),
            "substring" => inputText.Contains(mapping.ProviderCountryName, comparison),
            "regex" => TryRegexMatch(mapping.ProviderCountryName, inputText, mapping.IsCaseSensitive),
            _ => inputText.Contains(mapping.ProviderCountryName, comparison) // default to substring
        };
    }

    /// <summary>
    /// Safely try to match a regex pattern, returning false if the pattern is invalid
    /// </summary>
    private static bool TryRegexMatch(string pattern, string inputText, bool caseSensitive)
    {
        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return Regex.IsMatch(inputText, pattern, options, TimeSpan.FromMilliseconds(100));
        }
        catch
        {
            // Invalid regex pattern - return false instead of throwing
            return false;
        }
    }
}
