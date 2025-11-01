using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public class SyncWorkflowStateRepository : ISyncWorkflowStateRepository
{
    private readonly ConfigurationDbContext _context;

    public SyncWorkflowStateRepository(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncWorkflowState> GetOrCreateAsync()
    {
        var state = await _context.SyncWorkflowStates.FirstOrDefaultAsync();

        if (state == null)
        {
            state = new SyncWorkflowState();
            await _context.SyncWorkflowStates.AddAsync(state);
            await _context.SaveChangesAsync();
        }

        return state;
    }

    public async Task UpdateAsync(SyncWorkflowState state)
    {
        _context.SyncWorkflowStates.Update(state);
        await _context.SaveChangesAsync();
    }

    public async Task ResetAsync()
    {
        var state = await GetOrCreateAsync();
        state.Reset();
        await UpdateAsync(state);
    }
}
