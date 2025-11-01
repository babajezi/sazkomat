using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public class SyncWorkflowService : ISyncWorkflowService
{
    private readonly ISyncWorkflowStateRepository _workflowRepository;

    public SyncWorkflowService(ISyncWorkflowStateRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<Result<SyncWorkflowState>> GetStateAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();
            return Result<SyncWorkflowState>.Success(state);
        }
        catch (Exception ex)
        {
            return Result<SyncWorkflowState>.Failure($"Failed to get workflow state: {ex.Message}");
        }
    }

    public async Task<Result> MarkCountriesSyncedAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();
            state.CountriesSynced = true;
            state.CountriesSyncedAt = DateTime.UtcNow;
            await _workflowRepository.UpdateAsync(state);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to mark countries as synced: {ex.Message}");
        }
    }

    public async Task<Result> ConfirmCountriesAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();

            if (!state.CountriesSynced)
            {
                return Result.Failure("Countries must be synced before confirmation");
            }

            state.CountriesConfirmed = true;
            await _workflowRepository.UpdateAsync(state);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to confirm countries: {ex.Message}");
        }
    }

    public async Task<Result> MarkLeaguesSyncedAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();
            state.LeaguesSynced = true;
            state.LeaguesSyncedAt = DateTime.UtcNow;
            await _workflowRepository.UpdateAsync(state);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to mark leagues as synced: {ex.Message}");
        }
    }

    public async Task<Result> ConfirmLeaguesAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();

            if (!state.LeaguesSynced)
            {
                return Result.Failure("Leagues must be synced before confirmation");
            }

            state.LeaguesConfirmed = true;
            await _workflowRepository.UpdateAsync(state);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to confirm leagues: {ex.Message}");
        }
    }

    public async Task<Result> MarkSeasonsSyncedAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();
            state.SeasonsSynced = true;
            state.SeasonsSyncedAt = DateTime.UtcNow;
            await _workflowRepository.UpdateAsync(state);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to mark seasons as synced: {ex.Message}");
        }
    }

    public async Task<Result> ResetWorkflowAsync()
    {
        try
        {
            await _workflowRepository.ResetAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to reset workflow: {ex.Message}");
        }
    }

    public async Task<Result> CanSyncCountriesAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();

            if (state.CountriesSynced)
            {
                return Result.Failure("Countries have already been synced. Reset workflow to sync again.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to validate countries sync: {ex.Message}");
        }
    }

    public async Task<Result> CanSyncLeaguesAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();

            if (!state.CountriesConfirmed)
            {
                return Result.Failure("Countries must be confirmed before syncing leagues");
            }

            if (state.LeaguesSynced)
            {
                return Result.Failure("Leagues have already been synced. Reset workflow to sync again.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to validate leagues sync: {ex.Message}");
        }
    }

    public async Task<Result> CanSyncSeasonsAsync()
    {
        try
        {
            var state = await _workflowRepository.GetOrCreateAsync();

            if (!state.LeaguesConfirmed)
            {
                return Result.Failure("Leagues must be confirmed before syncing seasons");
            }

            if (state.SeasonsSynced)
            {
                return Result.Failure("Seasons have already been synced. Reset workflow to sync again.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to validate seasons sync: {ex.Message}");
        }
    }
}
