using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Repositories;

public interface ILeagueRepository
{
    Task<List<League>> GetAllAsync(Guid? sportId = null, Guid? countryId = null, bool? onlyEnabled = null, bool includeRelations = false);
    Task<League?> GetByIdAsync(Guid id);
    Task<League?> GetBySlugAsync(string slug);
    Task<League?> GetByBetExplorerSlugAsync(string betExplorerSlug);
    Task<List<League>> GetByCountryIdAsync(Guid countryId);
    Task<League> CreateAsync(League league);
    Task<League> UpdateAsync(League league);
    Task<League> AddAsync(League league);
    Task DeleteAsync(Guid id);
}
