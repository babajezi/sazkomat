using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Scrapers;
using Sazkomat.Core.Common;
using Sazkomat.Data.Services;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Adapter that implements IBetanoFullDataProvider using the BetanoScraper.
/// This breaks the circular dependency between DataImport and BettingProviders.
/// </summary>
public class BetanoFullDataProviderAdapter : IBetanoFullDataProvider
{
    private readonly BetanoScraper _scraper;
    private readonly ILogger<BetanoFullDataProviderAdapter> _logger;

    public BetanoFullDataProviderAdapter(
        BetanoScraper scraper,
        ILogger<BetanoFullDataProviderAdapter> logger)
    {
        _scraper = scraper;
        _logger = logger;
    }

    public async Task<Result<BetanoFullScanData>> GetFullDataAsync(string sportCode)
    {
        _logger.LogInformation("BetanoFullDataProviderAdapter: Getting full data for sport {SportCode}", sportCode);

        var result = await _scraper.GetFullDataAsync(sportCode);
        if (!result.IsSuccess)
        {
            return Result<BetanoFullScanData>.Failure(result.Error);
        }

        var scraperData = result.Value;

        // Map from scraper types to DataImport DTOs
        var fullScanData = new BetanoFullScanData
        {
            Regions = scraperData.Regions.Select(r => new BetanoRegionData
            {
                Code = r.Code,
                Name = r.Name,
                HasLeagues = r.HasLeagues
            }).ToList(),
            Leagues = scraperData.Leagues.Select(l => new BetanoLeagueData
            {
                ProviderLeagueName = l.ProviderLeagueName,
                ProviderLeagueId = l.ProviderLeagueId,
                ProviderUrl = l.ProviderUrl,
                CountryCode = l.CountryCode,
                CountryName = l.CountryName
            }).ToList()
        };

        _logger.LogInformation("BetanoFullDataProviderAdapter: Mapped {RegionCount} regions and {LeagueCount} leagues",
            fullScanData.Regions.Count, fullScanData.Leagues.Count);

        return Result<BetanoFullScanData>.Success(fullScanData);
    }
}
