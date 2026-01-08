using Sazkomat.Core.Common;

namespace Sazkomat.DataImport.Services;

/// <summary>
/// Interface for getting combined country and league data from Betano in a single HTTP request.
/// This interface breaks the circular dependency between DataImport and BettingProviders.
/// </summary>
public interface IBetanoFullDataProvider
{
    /// <summary>
    /// Gets both regions (countries) AND leagues in a single HTTP request.
    /// Optimized for combined scan to avoid duplicate fetches.
    /// </summary>
    Task<Result<BetanoFullScanData>> GetFullDataAsync(string sportCode);
}

/// <summary>
/// Result of full Betano scan containing both regions and leagues.
/// </summary>
public class BetanoFullScanData
{
    public List<BetanoRegionData> Regions { get; set; } = new();
    public List<BetanoLeagueData> Leagues { get; set; } = new();
}

/// <summary>
/// Region info from Betano
/// </summary>
public class BetanoRegionData
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasLeagues { get; set; }
}

/// <summary>
/// League info from Betano
/// </summary>
public class BetanoLeagueData
{
    public string ProviderLeagueName { get; set; } = string.Empty;
    public string? ProviderLeagueId { get; set; }
    public string? ProviderUrl { get; set; }
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
}
