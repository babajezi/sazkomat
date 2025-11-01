using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ISyncWorkflowStateRepository
{
    /// <summary>
    /// Gets the singleton workflow state. Creates a new one if it doesn't exist.
    /// </summary>
    Task<SyncWorkflowState> GetOrCreateAsync();

    /// <summary>
    /// Updates the workflow state
    /// </summary>
    Task UpdateAsync(SyncWorkflowState state);

    /// <summary>
    /// Resets the workflow state to initial values
    /// </summary>
    Task ResetAsync();
}
