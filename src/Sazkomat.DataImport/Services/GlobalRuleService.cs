using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Helpers;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Service for creating and managing global mapping rules.
/// Global rules (ProviderCode = "*") apply to all betting providers.
/// </summary>
public interface IGlobalRuleService
{
    /// <summary>
    /// Gets a preview of what would happen if a global rule was created from the given mapped league.
    /// </summary>
    Task<GlobalRulePreview> GetGlobalRulePreviewAsync(Guid sourceUnmatchedLeagueId);

    /// <summary>
    /// Creates a global rule from a mapped unmatched league and resolves all affected leagues.
    /// </summary>
    Task<GlobalRuleResult> CreateGlobalRuleAsync(CreateGlobalRuleRequest request);
}

public class GlobalRuleService : IGlobalRuleService
{
    private readonly IUnmatchedLeagueRepository _unmatchedLeagueRepo;
    private readonly ILeagueNameMappingRepository _mappingRepo;
    private readonly ILeagueRepository _leagueRepo;
    private readonly IDataProviderRepository _providerRepo;
    private readonly ILogger<GlobalRuleService> _logger;

    public GlobalRuleService(
        IUnmatchedLeagueRepository unmatchedLeagueRepo,
        ILeagueNameMappingRepository mappingRepo,
        ILeagueRepository leagueRepo,
        IDataProviderRepository providerRepo,
        ILogger<GlobalRuleService> logger)
    {
        _unmatchedLeagueRepo = unmatchedLeagueRepo;
        _mappingRepo = mappingRepo;
        _leagueRepo = leagueRepo;
        _providerRepo = providerRepo;
        _logger = logger;
    }

    public async Task<GlobalRulePreview> GetGlobalRulePreviewAsync(Guid sourceUnmatchedLeagueId)
    {
        // 1. Load source unmatched league
        var source = await _unmatchedLeagueRepo.GetByIdAsync(sourceUnmatchedLeagueId);
        if (source == null)
        {
            return new GlobalRulePreview
            {
                CanCreateGlobalRule = false,
                ValidationMessage = "Zdrojová liga nebyla nalezena."
            };
        }

        // 2. Verify it's resolved as Mapped
        if (!source.IsResolved || source.ResolutionType != ResolutionType.Mapped || source.ResolvedLeagueId == null)
        {
            return new GlobalRulePreview
            {
                CanCreateGlobalRule = false,
                ValidationMessage = "Liga musí být namapována na existující ligu."
            };
        }

        // 3. Get the resolved league
        var resolvedLeague = await _leagueRepo.GetByIdAsync(source.ResolvedLeagueId.Value);
        if (resolvedLeague == null)
        {
            return new GlobalRulePreview
            {
                CanCreateGlobalRule = false,
                ValidationMessage = "Cílová liga nebyla nalezena."
            };
        }

        // 4. Normalize the league name
        var normalizedName = LeagueNameNormalizer.Normalize(source.ProviderLeagueName);

        // 5. Check if global rule already exists
        var existingGlobalRule = await _mappingRepo.FindMappingWithFallbackAsync(
            LeagueNameMapping.GlobalProviderCode,
            source.CountryCode.ToLowerInvariant(),
            source.ProviderLeagueName);

        if (existingGlobalRule != null && existingGlobalRule.IsGlobal)
        {
            return new GlobalRulePreview
            {
                NormalizedLeagueName = normalizedName,
                CountryCode = source.CountryCode,
                BetExplorerSlug = resolvedLeague.BetExplorerSlug,
                SourceLeagueId = resolvedLeague.Id,
                SourceLeagueName = resolvedLeague.Name,
                AffectedLeagues = new List<AffectedUnmatchedLeague>(),
                CanCreateGlobalRule = false,
                ValidationMessage = $"Globální pravidlo pro tuto ligu již existuje (mapuje na {existingGlobalRule.BetExplorerSlug})."
            };
        }

        // 6. Find all affected unmatched leagues (same normalized name + country)
        var allUnmatched = await _unmatchedLeagueRepo.GetAllAsync();
        var providers = await _providerRepo.GetAllAsync();
        var providerDict = providers.ToDictionary(p => p.Id, p => p.Name);

        var affected = allUnmatched
            .Where(ul =>
                ul.CountryCode.Equals(source.CountryCode, StringComparison.OrdinalIgnoreCase) &&
                LeagueNameNormalizer.AreEquivalent(ul.ProviderLeagueName, source.ProviderLeagueName))
            .Select(ul => new AffectedUnmatchedLeague
            {
                Id = ul.Id,
                ProviderName = providerDict.GetValueOrDefault(ul.ProviderId, "Unknown"),
                ProviderLeagueName = ul.ProviderLeagueName,
                IsResolved = ul.IsResolved,
                ResolutionType = ul.ResolutionType?.ToString()
            })
            .ToList();

        return new GlobalRulePreview
        {
            NormalizedLeagueName = normalizedName,
            CountryCode = source.CountryCode,
            BetExplorerSlug = resolvedLeague.BetExplorerSlug,
            SourceLeagueId = resolvedLeague.Id,
            SourceLeagueName = resolvedLeague.Name,
            AffectedLeagues = affected,
            CanCreateGlobalRule = true
        };
    }

    public async Task<GlobalRuleResult> CreateGlobalRuleAsync(CreateGlobalRuleRequest request)
    {
        // 1. Get the preview to validate
        var preview = await GetGlobalRulePreviewAsync(request.SourceUnmatchedLeagueId);

        if (!preview.CanCreateGlobalRule)
        {
            throw new InvalidOperationException(preview.ValidationMessage ?? "Nelze vytvořit globální pravidlo.");
        }

        // 2. Load source unmatched league
        var source = await _unmatchedLeagueRepo.GetByIdAsync(request.SourceUnmatchedLeagueId);
        if (source == null || source.ResolvedLeagueId == null)
        {
            throw new InvalidOperationException("Zdrojová liga nebyla nalezena.");
        }

        // 3. Create the global mapping rule
        var globalMapping = new LeagueNameMapping
        {
            ProviderCode = LeagueNameMapping.GlobalProviderCode,
            CountryCode = source.CountryCode.ToLowerInvariant(),
            ProviderLeagueName = source.ProviderLeagueName,
            NormalizedProviderLeagueName = LeagueNameNormalizer.Normalize(source.ProviderLeagueName),
            BetExplorerSlug = preview.BetExplorerSlug!,
            IsActive = true,
            Priority = 100, // Lower priority than provider-specific rules
            Notes = request.Notes ?? $"Global rule created from {source.ProviderLeagueName}"
        };

        var createdMapping = await _mappingRepo.CreateAsync(globalMapping);

        _logger.LogInformation(
            "Created global rule: '{LeagueName}' -> '{Slug}' (country: {Country})",
            source.ProviderLeagueName,
            preview.BetExplorerSlug,
            source.CountryCode);

        // 4. Delete all affected unmatched leagues (they are now handled by global rule)
        int deletedCount = 0;
        foreach (var affected in preview.AffectedLeagues)
        {
            try
            {
                await _unmatchedLeagueRepo.DeleteAsync(affected.Id);
                deletedCount++;

                _logger.LogDebug(
                    "Deleted unmatched league {Id} ({Provider}: {Name}) - now handled by global rule",
                    affected.Id,
                    affected.ProviderName,
                    affected.ProviderLeagueName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to delete unmatched league {Id}",
                    affected.Id);
            }
        }

        return new GlobalRuleResult
        {
            GlobalRuleId = createdMapping.Id,
            DeletedCount = deletedCount
        };
    }
}

/// <summary>
/// Preview of what a global rule would do.
/// </summary>
public class GlobalRulePreview
{
    public string? NormalizedLeagueName { get; set; }
    public string? CountryCode { get; set; }
    public string? BetExplorerSlug { get; set; }
    public Guid? SourceLeagueId { get; set; }
    public string? SourceLeagueName { get; set; }
    public List<AffectedUnmatchedLeague> AffectedLeagues { get; set; } = new();
    public bool CanCreateGlobalRule { get; set; }
    public string? ValidationMessage { get; set; }
}

/// <summary>
/// An unmatched league that would be affected by a global rule.
/// </summary>
public class AffectedUnmatchedLeague
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderLeagueName { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public string? ResolutionType { get; set; }
}

/// <summary>
/// Request to create a global rule.
/// </summary>
public class CreateGlobalRuleRequest
{
    public Guid SourceUnmatchedLeagueId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of creating a global rule.
/// </summary>
public class GlobalRuleResult
{
    public Guid GlobalRuleId { get; set; }
    public int DeletedCount { get; set; }
}
