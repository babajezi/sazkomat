using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public interface IProviderService
{
    // DataProvider management
    Task<Result<DataProvider>> CreateDataProviderAsync(CreateDataProviderRequest request);
    Task<Result<DataProvider>> UpdateDataProviderAsync(Guid providerId, UpdateDataProviderRequest request);
    Task<Result> DeleteDataProviderAsync(Guid providerId);
    Task<Result<DataProvider>> UpdateProviderCredentialsAsync(Guid providerId, UpdateProviderCredentialsRequest request);
    Task<Result<DataProvider>> UpdateProviderConfigurationAsync(Guid providerId, UpdateProviderConfigurationRequest request);

    // CountryProvider management
    Task<Result<CountryProvider>> CreateCountryProviderAsync(CreateCountryProviderRequest request);
    Task<Result<CountryProvider>> UpdateCountryProviderAsync(Guid countryProviderId, UpdateCountryProviderRequest request);
    Task<Result> DeleteCountryProviderAsync(Guid countryProviderId);

    // LeagueProvider management
    Task<Result<LeagueProvider>> CreateLeagueProviderAsync(CreateLeagueProviderRequest request);
    Task<Result<LeagueProvider>> UpdateLeagueProviderAsync(Guid leagueProviderId, UpdateLeagueProviderRequest request);
    Task<Result> DeleteLeagueProviderAsync(Guid leagueProviderId);
    Task<Result<LeagueProvider>> ActivateLeagueProviderAsync(Guid leagueProviderId);
}
