using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public interface ISyncWorkflowService
{
    /// <summary>
    /// Gets the current workflow state
    /// </summary>
    Task<Result<SyncWorkflowState>> GetStateAsync();

    /// <summary>
    /// Marks countries as synced
    /// </summary>
    Task<Result> MarkCountriesSyncedAsync();

    /// <summary>
    /// Confirms country selection (user has marked active countries)
    /// </summary>
    Task<Result> ConfirmCountriesAsync();

    /// <summary>
    /// Marks leagues as synced
    /// </summary>
    Task<Result> MarkLeaguesSyncedAsync();

    /// <summary>
    /// Confirms league selection (user has marked active/bettable leagues)
    /// </summary>
    Task<Result> ConfirmLeaguesAsync();

    /// <summary>
    /// Marks seasons as synced
    /// </summary>
    Task<Result> MarkSeasonsSyncedAsync();

    /// <summary>
    /// Resets the entire workflow
    /// </summary>
    Task<Result> ResetWorkflowAsync();

    /// <summary>
    /// Validates if countries can be synced
    /// </summary>
    Task<Result> CanSyncCountriesAsync();

    /// <summary>
    /// Validates if leagues can be synced
    /// </summary>
    Task<Result> CanSyncLeaguesAsync();

    /// <summary>
    /// Validates if seasons can be synced
    /// </summary>
    Task<Result> CanSyncSeasonsAsync();
}
