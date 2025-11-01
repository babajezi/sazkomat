using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class ImportJobRepository : IImportJobRepository
{
    private readonly DataImportDbContext _context;
    private readonly ILogger<ImportJobRepository> _logger;

    public ImportJobRepository(DataImportDbContext context, ILogger<ImportJobRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ImportJob>> GetAllAsync()
    {
        return await _context.ImportJobs
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task<ImportJob?> GetByIdAsync(Guid id)
    {
        return await _context.ImportJobs
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<List<ImportJob>> GetByLeagueIdAsync(Guid leagueId)
    {
        return await _context.ImportJobs
            .Where(j => j.LeagueId == leagueId)
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task<ImportJob> CreateAsync(ImportJob job)
    {
        _logger.LogDebug("Creating import job {JobId} for league {LeagueId} in database", job.Id, job.LeagueId);
        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully created import job {JobId} in database", job.Id);
        return job;
    }

    public async Task<ImportJob> UpdateAsync(ImportJob job)
    {
        _logger.LogDebug("Updating import job {JobId} (Status: {Status}) in database", job.Id, job.Status);
        _context.ImportJobs.Update(job);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Successfully updated import job {JobId} in database", job.Id);
        return job;
    }
}
