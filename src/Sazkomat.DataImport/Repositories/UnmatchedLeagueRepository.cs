using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class UnmatchedLeagueRepository : IUnmatchedLeagueRepository
{
    private readonly DataImportDbContext _context;

    public UnmatchedLeagueRepository(DataImportDbContext context)
    {
        _context = context;
    }

    public async Task<UnmatchedLeague?> GetByIdAsync(Guid id)
    {
        // Note: Provider and ResolvedLeague are in configuration schema, not data_import
        // They need to be loaded separately if needed
        return await _context.UnmatchedLeagues
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<UnmatchedLeague>> GetAllAsync()
    {
        return await _context.UnmatchedLeagues
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedLeague>> GetUnresolvedAsync()
    {
        return await _context.UnmatchedLeagues
            .Where(u => !u.IsResolved)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedLeague>> GetByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedLeagues
            .Where(u => u.ProviderId == providerId)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedLeague>> GetUnresolvedByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedLeagues
            .Where(u => u.ProviderId == providerId && !u.IsResolved)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedLeague>> GetResolvedAsMappedByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedLeagues
            .Where(u => u.ProviderId == providerId
                     && u.IsResolved
                     && u.ResolutionType == ResolutionType.Mapped
                     && u.ResolvedLeagueId.HasValue)
            .OrderByDescending(u => u.ResolvedAt)
            .ToListAsync();
    }

    public async Task<UnmatchedLeague?> FindExistingAsync(Guid providerId, string providerLeagueName, string countryCode)
    {
        return await _context.UnmatchedLeagues
            .FirstOrDefaultAsync(u =>
                u.ProviderId == providerId &&
                u.ProviderLeagueName == providerLeagueName &&
                u.CountryCode == countryCode);
    }

    public async Task<UnmatchedLeague> CreateAsync(UnmatchedLeague unmatchedLeague)
    {
        _context.UnmatchedLeagues.Add(unmatchedLeague);
        await _context.SaveChangesAsync();
        return unmatchedLeague;
    }

    public async Task<UnmatchedLeague> UpdateAsync(UnmatchedLeague unmatchedLeague)
    {
        unmatchedLeague.UpdatedAt = DateTime.UtcNow;
        _context.UnmatchedLeagues.Update(unmatchedLeague);
        await _context.SaveChangesAsync();
        return unmatchedLeague;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.UnmatchedLeagues.FindAsync(id);
        if (entity != null)
        {
            _context.UnmatchedLeagues.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UnmatchedLeague> ResolveAsMappedAsync(Guid id, Guid leagueId, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedLeague with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Mapped;
        entity.ResolvedLeagueId = leagueId;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedLeagues.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<UnmatchedLeague> ResolveAsIgnoredAsync(Guid id, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedLeague with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Ignored;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedLeagues.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<UnmatchedLeague> ResolveAsUnavailableAsync(Guid id, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedLeague with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Unavailable;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedLeagues.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}
