using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public class UnmatchedCountryRepository : IUnmatchedCountryRepository
{
    private readonly DataDbContext _context;

    public UnmatchedCountryRepository(DataDbContext context)
    {
        _context = context;
    }

    public async Task<UnmatchedCountry?> GetByIdAsync(Guid id)
    {
        return await _context.UnmatchedCountries
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<UnmatchedCountry>> GetAllAsync()
    {
        return await _context.UnmatchedCountries
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedCountry>> GetUnresolvedAsync()
    {
        return await _context.UnmatchedCountries
            .Where(u => !u.IsResolved)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedCountry>> GetByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedCountries
            .Where(u => u.ProviderId == providerId)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedCountry>> GetUnresolvedByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedCountries
            .Where(u => u.ProviderId == providerId && !u.IsResolved)
            .OrderByDescending(u => u.ScrapedAt)
            .ToListAsync();
    }

    public async Task<List<UnmatchedCountry>> GetResolvedAsMappedByProviderAsync(Guid providerId)
    {
        return await _context.UnmatchedCountries
            .Where(u => u.ProviderId == providerId
                     && u.IsResolved
                     && u.ResolutionType == ResolutionType.Mapped
                     && u.ResolvedCountryId.HasValue)
            .OrderByDescending(u => u.ResolvedAt)
            .ToListAsync();
    }

    public async Task<UnmatchedCountry?> FindExistingAsync(Guid providerId, string providerCountryName)
    {
        return await _context.UnmatchedCountries
            .FirstOrDefaultAsync(u =>
                u.ProviderId == providerId &&
                u.ProviderCountryName == providerCountryName);
    }

    public async Task<UnmatchedCountry> CreateAsync(UnmatchedCountry unmatchedCountry)
    {
        _context.UnmatchedCountries.Add(unmatchedCountry);
        await _context.SaveChangesAsync();
        return unmatchedCountry;
    }

    public async Task<UnmatchedCountry> UpdateAsync(UnmatchedCountry unmatchedCountry)
    {
        unmatchedCountry.UpdatedAt = DateTime.UtcNow;
        _context.UnmatchedCountries.Update(unmatchedCountry);
        await _context.SaveChangesAsync();
        return unmatchedCountry;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.UnmatchedCountries.FindAsync(id);
        if (entity != null)
        {
            _context.UnmatchedCountries.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UnmatchedCountry> ResolveAsMappedAsync(Guid id, Guid countryId, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedCountry with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Mapped;
        entity.ResolvedCountryId = countryId;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedCountries.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<UnmatchedCountry> ResolveAsIgnoredAsync(Guid id, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedCountry with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Ignored;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedCountries.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<UnmatchedCountry> ResolveAsUnavailableAsync(Guid id, string? notes = null)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedCountry with ID {id} not found");

        entity.IsResolved = true;
        entity.ResolutionType = Entities.ResolutionType.Unavailable;
        entity.ResolvedAt = DateTime.UtcNow;
        entity.ResolutionNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedCountries.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UnresolveAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            throw new ArgumentException($"UnmatchedCountry with ID {id} not found");

        entity.IsResolved = false;
        entity.ResolutionType = null;
        entity.ResolvedCountryId = null;
        entity.ResolvedAt = null;
        entity.ResolutionNotes = null;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.UnmatchedCountries.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<UnmatchedCountryStats> GetStatsAsync(Guid? providerId = null)
    {
        var query = _context.UnmatchedCountries.AsQueryable();

        if (providerId.HasValue)
        {
            query = query.Where(u => u.ProviderId == providerId.Value);
        }

        var all = await query.ToListAsync();

        return new UnmatchedCountryStats
        {
            Total = all.Count,
            Unresolved = all.Count(u => !u.IsResolved),
            Mapped = all.Count(u => u.IsResolved && u.ResolutionType == ResolutionType.Mapped),
            Ignored = all.Count(u => u.IsResolved && u.ResolutionType == ResolutionType.Ignored),
            Unavailable = all.Count(u => u.IsResolved && u.ResolutionType == ResolutionType.Unavailable)
        };
    }
}
