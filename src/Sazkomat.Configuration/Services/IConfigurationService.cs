using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public interface IConfigurationService
{
    Task<Result<Sport>> UpdateSportAsync(Guid sportId, UpdateSportRequest request);

    Task<Result<League>> CreateLeagueAsync(CreateLeagueRequest request);
    Task<Result<League>> UpdateLeagueAsync(Guid leagueId, UpdateLeagueRequest request);
    Task<Result> DeleteLeagueAsync(Guid leagueId);

    Task<Result<Country>> CreateCountryAsync(CreateCountryRequest request);
    Task<Result<Country>> UpdateCountryAsync(Guid countryId, UpdateCountryRequest request);
    Task<Result> DeleteCountryAsync(Guid countryId);

    Task<Result<CountryProvider>> ToggleCountryProviderSyncAsync(Guid countryId, Guid providerId, bool isActive);
    Task<Result<LeagueProvider>> ToggleLeagueProviderSyncAsync(Guid leagueId, Guid providerId, bool isActive);
}
