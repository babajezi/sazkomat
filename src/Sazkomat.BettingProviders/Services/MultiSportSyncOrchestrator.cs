using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Scrapers;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Orchestrates synchronization of multiple sports in parallel or sequentially
/// </summary>
public class MultiSportSyncOrchestrator
{
    private readonly BettingProviderOrchestrator _providerOrchestrator;
    private readonly ConfigurationDbContext _context;
    private readonly ILogger<MultiSportSyncOrchestrator> _logger;
    private readonly Random _random = new();

    public MultiSportSyncOrchestrator(
        BettingProviderOrchestrator providerOrchestrator,
        ConfigurationDbContext context,
        ILogger<MultiSportSyncOrchestrator> logger)
    {
        _providerOrchestrator = providerOrchestrator;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Syncs all active sports for a given provider
    /// </summary>
    public async Task<Result<MultiSportSyncResult>> SyncAllActiveSportsAsync(
        string providerCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting multi-sport sync for provider: {Provider}", providerCode);

            // Get provider
            var provider = await _context.DataProviders
                .FirstOrDefaultAsync(p => p.Code.ToLower() == providerCode.ToLower(), cancellationToken);

            if (provider == null)
            {
                return Result<MultiSportSyncResult>.Failure($"Provider '{providerCode}' not found");
            }

            if (!provider.IsActive)
            {
                return Result<MultiSportSyncResult>.Failure($"Provider '{providerCode}' is not active");
            }

            // Get all active sports with provider mappings
            var sports = await _context.Sports
                .Include(s => s.SportProviders)
                .Where(s => s.IsActive && s.SportProviders.Any(sp => sp.ProviderId == provider.Id && sp.IsActive))
                .OrderByDescending(s => s.Priority)
                .ToListAsync(cancellationToken);

            if (!sports.Any())
            {
                _logger.LogWarning("No active sports found for provider {Provider}", providerCode);
                return Result<MultiSportSyncResult>.Success(new MultiSportSyncResult
                {
                    ProviderCode = providerCode,
                    TotalSports = 0,
                    SuccessfulSports = 0,
                    FailedSports = 0
                });
            }

            _logger.LogInformation("Found {Count} active sports for {Provider}", sports.Count, providerCode);

            var result = new MultiSportSyncResult
            {
                ProviderCode = providerCode,
                TotalSports = sports.Count
            };

            // Sync each sport sequentially (with human-like delays)
            foreach (var sport in sports)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Multi-sport sync cancelled");
                    break;
                }

                var sportResult = await SyncSportAsync(sport, provider, cancellationToken);
                result.SportResults.Add(sportResult);

                if (sportResult.Success)
                {
                    result.SuccessfulSports++;
                }
                else
                {
                    result.FailedSports++;
                }

                // Human-like delay between sports (5-10 seconds)
                if (sport != sports.Last())
                {
                    var delay = _random.Next(5000, 10000);
                    _logger.LogDebug("Waiting {Delay}ms before next sport sync", delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            _logger.LogInformation(
                "Multi-sport sync completed for {Provider}. Success: {Success}/{Total}",
                providerCode, result.SuccessfulSports, result.TotalSports);

            return Result<MultiSportSyncResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-sport sync for provider {Provider}", providerCode);
            return Result<MultiSportSyncResult>.Failure($"Multi-sport sync failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Syncs specific sports for a provider
    /// </summary>
    public async Task<Result<MultiSportSyncResult>> SyncSelectedSportsAsync(
        string providerCode,
        List<string> sportCodes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting selective sport sync for provider: {Provider}, sports: {Sports}",
                providerCode, string.Join(", ", sportCodes));

            // Get provider
            var provider = await _context.DataProviders
                .FirstOrDefaultAsync(p => p.Code.ToLower() == providerCode.ToLower(), cancellationToken);

            if (provider == null)
            {
                return Result<MultiSportSyncResult>.Failure($"Provider '{providerCode}' not found");
            }

            // Get selected sports
            var sports = await _context.Sports
                .Include(s => s.SportProviders)
                .Where(s => sportCodes.Contains(s.Code.ToLower()) &&
                           s.SportProviders.Any(sp => sp.ProviderId == provider.Id && sp.IsActive))
                .OrderByDescending(s => s.Priority)
                .ToListAsync(cancellationToken);

            if (!sports.Any())
            {
                return Result<MultiSportSyncResult>.Failure("No matching sports found");
            }

            var result = new MultiSportSyncResult
            {
                ProviderCode = providerCode,
                TotalSports = sports.Count
            };

            foreach (var sport in sports)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var sportResult = await SyncSportAsync(sport, provider, cancellationToken);
                result.SportResults.Add(sportResult);

                if (sportResult.Success)
                    result.SuccessfulSports++;
                else
                    result.FailedSports++;

                // Delay between sports
                if (sport != sports.Last())
                {
                    await Task.Delay(_random.Next(5000, 10000), cancellationToken);
                }
            }

            return Result<MultiSportSyncResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during selective sport sync");
            return Result<MultiSportSyncResult>.Failure($"Selective sync failed: {ex.Message}");
        }
    }

    private async Task<SportSyncResult> SyncSportAsync(
        Sport sport,
        DataProvider provider,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new SportSyncResult
        {
            SportCode = sport.Code,
            SportName = sport.Name
        };

        try
        {
            _logger.LogInformation("Syncing sport: {Sport} for provider: {Provider}",
                sport.Name, provider.Name);

            // Sync leagues for this sport using existing orchestrator method
            var syncResult = await _providerOrchestrator.SyncLeagueAvailabilityAsync(
                provider.Code, sport.Code.ToLower());

            if (syncResult.IsSuccess)
            {
                result.Success = true;
                // Count leagues - get from database after sync
                var leagueProviders = await _context.LeagueProviders
                    .Where(lp => lp.ProviderId == provider.Id &&
                                 lp.League.Sport.Code.ToLower() == sport.Code.ToLower())
                    .CountAsync(cancellationToken);

                result.LeaguesFound = leagueProviders;
                result.Duration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Successfully synced {Sport}: {Count} leagues in {Duration}ms",
                    sport.Name, result.LeaguesFound, result.Duration.TotalMilliseconds);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = syncResult.Error;
                _logger.LogError("Failed to sync {Sport}: {Error}", sport.Name, syncResult.Error);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Exception during sport sync: {Sport}", sport.Name);
        }

        return result;
    }
}

/// <summary>
/// Result of multi-sport synchronization
/// </summary>
public class MultiSportSyncResult
{
    public string ProviderCode { get; set; } = string.Empty;
    public int TotalSports { get; set; }
    public int SuccessfulSports { get; set; }
    public int FailedSports { get; set; }
    public List<SportSyncResult> SportResults { get; set; } = new();
}

/// <summary>
/// Result of single sport synchronization
/// </summary>
public class SportSyncResult
{
    public string SportCode { get; set; } = string.Empty;
    public string SportName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int LeaguesFound { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}
