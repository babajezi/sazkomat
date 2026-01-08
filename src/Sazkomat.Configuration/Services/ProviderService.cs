using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public class ProviderService : IProviderService
{
    private readonly IDataProviderRepository _dataProviderRepository;
    private readonly ILeagueProviderRepository _leagueProviderRepository;
    private readonly ICountryProviderRepository _countryProviderRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(
        IDataProviderRepository dataProviderRepository,
        ILeagueProviderRepository leagueProviderRepository,
        ICountryProviderRepository countryProviderRepository,
        ICountryRepository countryRepository,
        ILeagueRepository leagueRepository,
        ILogger<ProviderService> logger)
    {
        _dataProviderRepository = dataProviderRepository;
        _leagueProviderRepository = leagueProviderRepository;
        _countryProviderRepository = countryProviderRepository;
        _countryRepository = countryRepository;
        _leagueRepository = leagueRepository;
        _logger = logger;
    }

    // DataProvider management

    public async Task<Result<DataProvider>> CreateDataProviderAsync(CreateDataProviderRequest request)
    {
        // Check for duplicate code
        var existingProvider = await _dataProviderRepository.GetByCodeAsync(request.Code);
        if (existingProvider != null)
        {
            return Result<DataProvider>.Failure($"Data provider with code '{request.Code}' already exists");
        }

        var provider = new DataProvider
        {
            Name = request.Name,
            Code = request.Code,
            BaseUrl = request.BaseUrl,
            Type = request.Type,
            IsActive = request.IsActive,
            Priority = request.Priority,
            Notes = request.Notes
        };

        await _dataProviderRepository.AddAsync(provider);
        return Result<DataProvider>.Success(provider);
    }

    public async Task<Result<DataProvider>> UpdateDataProviderAsync(Guid providerId, UpdateDataProviderRequest request)
    {
        var provider = await _dataProviderRepository.GetByIdAsync(providerId);
        if (provider == null)
        {
            return Result<DataProvider>.Failure($"Data provider with ID {providerId} not found");
        }

        // Update only provided fields
        if (request.Name != null)
            provider.Name = request.Name;

        if (request.Code != null)
        {
            // Check for duplicate code
            var existingProvider = await _dataProviderRepository.GetByCodeAsync(request.Code);
            if (existingProvider != null && existingProvider.Id != providerId)
            {
                return Result<DataProvider>.Failure($"Data provider with code '{request.Code}' already exists");
            }
            provider.Code = request.Code;
        }

        if (request.BaseUrl != null)
            provider.BaseUrl = request.BaseUrl;

        if (request.Type.HasValue)
            provider.Type = request.Type.Value;

        if (request.IsActive.HasValue)
            provider.IsActive = request.IsActive.Value;

        if (request.Priority.HasValue)
            provider.Priority = request.Priority.Value;

        if (request.Notes != null)
            provider.Notes = request.Notes;

        await _dataProviderRepository.UpdateAsync(provider);
        return Result<DataProvider>.Success(provider);
    }

    public async Task<Result> DeleteDataProviderAsync(Guid providerId)
    {
        var provider = await _dataProviderRepository.GetByIdAsync(providerId);
        if (provider == null)
        {
            return Result.Failure($"Data provider with ID {providerId} not found");
        }

        await _dataProviderRepository.DeleteAsync(providerId);
        return Result.Success();
    }

    public async Task<Result<DataProvider>> UpdateProviderCredentialsAsync(Guid providerId, UpdateProviderCredentialsRequest request)
    {
        var provider = await _dataProviderRepository.GetByIdAsync(providerId);
        if (provider == null)
        {
            return Result<DataProvider>.Failure($"Data provider with ID {providerId} not found");
        }

        // Build credentials JSON
        var credentials = new
        {
            Username = request.Username,
            Password = request.Password,
            SessionCookies = request.SessionCookies
        };

        provider.Credentials = System.Text.Json.JsonSerializer.Serialize(credentials);
        await _dataProviderRepository.UpdateAsync(provider);
        return Result<DataProvider>.Success(provider);
    }

    public async Task<Result<DataProvider>> UpdateProviderConfigurationAsync(Guid providerId, UpdateProviderConfigurationRequest request)
    {
        var provider = await _dataProviderRepository.GetByIdAsync(providerId);
        if (provider == null)
        {
            return Result<DataProvider>.Failure($"Data provider with ID {providerId} not found");
        }

        // Build configuration JSON
        var config = new
        {
            Timeout = request.Timeout,
            ProxyUrl = request.ProxyUrl,
            ExcludedCountryIds = request.ExcludedCountryIds,
            ExcludedLeagueIds = request.ExcludedLeagueIds,
            CustomSettings = request.CustomSettings
        };

        provider.Configuration = System.Text.Json.JsonSerializer.Serialize(config);
        await _dataProviderRepository.UpdateAsync(provider);
        return Result<DataProvider>.Success(provider);
    }

    // CountryProvider management

    public async Task<Result<CountryProvider>> CreateCountryProviderAsync(CreateCountryProviderRequest request)
    {
        // Validate country exists
        var country = await _countryRepository.GetByIdAsync(request.CountryId);
        if (country == null)
        {
            return Result<CountryProvider>.Failure($"Country with ID {request.CountryId} not found");
        }

        // Validate provider exists
        var provider = await _dataProviderRepository.GetByIdAsync(request.ProviderId);
        if (provider == null)
        {
            return Result<CountryProvider>.Failure($"Data provider with ID {request.ProviderId} not found");
        }

        // Check for duplicate mapping (country + provider combination must be unique)
        var existingMapping = await _countryProviderRepository.GetByCountryAndProviderAsync(request.CountryId, request.ProviderId);
        if (existingMapping != null)
        {
            return Result<CountryProvider>.Failure($"Country-provider mapping already exists for {country.Name} and {provider.Name}");
        }

        var countryProvider = new CountryProvider
        {
            CountryId = request.CountryId,
            ProviderId = request.ProviderId,
            ProviderCode = request.ProviderCode,
            ProviderName = request.ProviderName,
            IsActive = request.IsActive,
            Metadata = request.Metadata
        };

        await _countryProviderRepository.AddAsync(countryProvider);

        // Auto-activate country when creating CountryProvider mapping for betting provider
        if (provider.Type == ProviderType.BettingProvider && !country.IsActive)
        {
            country.IsActive = true;
            await _countryRepository.UpdateAsync(country);
            _logger.LogInformation("Auto-activated country {CountryName} ({CountryCode}) due to betting provider mapping",
                country.Name, country.Code);
        }

        return Result<CountryProvider>.Success(countryProvider);
    }

    public async Task<Result<CountryProvider>> UpdateCountryProviderAsync(Guid countryProviderId, UpdateCountryProviderRequest request)
    {
        var countryProvider = await _countryProviderRepository.GetByIdAsync(countryProviderId);
        if (countryProvider == null)
        {
            return Result<CountryProvider>.Failure($"Country-provider mapping with ID {countryProviderId} not found");
        }

        // Update only provided fields
        if (request.ProviderCode != null)
            countryProvider.ProviderCode = request.ProviderCode;

        if (request.ProviderName != null)
            countryProvider.ProviderName = request.ProviderName;

        if (request.IsActive.HasValue)
            countryProvider.IsActive = request.IsActive.Value;

        if (request.Metadata != null)
            countryProvider.Metadata = request.Metadata;

        await _countryProviderRepository.UpdateAsync(countryProvider);
        return Result<CountryProvider>.Success(countryProvider);
    }

    public async Task<Result> DeleteCountryProviderAsync(Guid countryProviderId)
    {
        var countryProvider = await _countryProviderRepository.GetByIdAsync(countryProviderId);
        if (countryProvider == null)
        {
            return Result.Failure($"Country-provider mapping with ID {countryProviderId} not found");
        }

        await _countryProviderRepository.DeleteAsync(countryProviderId);
        return Result.Success();
    }

    // LeagueProvider management

    public async Task<Result<LeagueProvider>> CreateLeagueProviderAsync(CreateLeagueProviderRequest request)
    {
        // Validate league exists
        var league = await _leagueRepository.GetByIdAsync(request.LeagueId);
        if (league == null)
        {
            return Result<LeagueProvider>.Failure($"League with ID {request.LeagueId} not found");
        }

        // Validate provider exists
        var provider = await _dataProviderRepository.GetByIdAsync(request.ProviderId);
        if (provider == null)
        {
            return Result<LeagueProvider>.Failure($"Data provider with ID {request.ProviderId} not found");
        }

        // Check for duplicate mapping
        var existingMapping = await _leagueProviderRepository.GetByLeagueAndProviderAsync(request.LeagueId, request.ProviderId);
        if (existingMapping != null)
        {
            return Result<LeagueProvider>.Failure($"League-provider mapping already exists for {league.Name} and {provider.Name}");
        }

        var leagueProvider = new LeagueProvider
        {
            LeagueId = request.LeagueId,
            ProviderId = request.ProviderId,
            ProviderSlug = request.ProviderSlug,
            ProviderName = request.ProviderName,
            IsActive = request.IsActive
        };

        await _leagueProviderRepository.AddAsync(leagueProvider);
        return Result<LeagueProvider>.Success(leagueProvider);
    }

    public async Task<Result<LeagueProvider>> UpdateLeagueProviderAsync(Guid leagueProviderId, UpdateLeagueProviderRequest request)
    {
        var leagueProvider = await _leagueProviderRepository.GetByIdAsync(leagueProviderId);
        if (leagueProvider == null)
        {
            return Result<LeagueProvider>.Failure($"League-provider mapping with ID {leagueProviderId} not found");
        }

        // Update only provided fields
        if (request.ProviderSlug != null)
            leagueProvider.ProviderSlug = request.ProviderSlug;

        if (request.ProviderName != null)
            leagueProvider.ProviderName = request.ProviderName;

        if (request.IsActive.HasValue)
            leagueProvider.IsActive = request.IsActive.Value;

        await _leagueProviderRepository.UpdateAsync(leagueProvider);
        return Result<LeagueProvider>.Success(leagueProvider);
    }

    public async Task<Result> DeleteLeagueProviderAsync(Guid leagueProviderId)
    {
        var leagueProvider = await _leagueProviderRepository.GetByIdAsync(leagueProviderId);
        if (leagueProvider == null)
        {
            return Result.Failure($"League-provider mapping with ID {leagueProviderId} not found");
        }

        await _leagueProviderRepository.DeleteAsync(leagueProviderId);
        return Result.Success();
    }

    public async Task<Result<LeagueProvider>> ActivateLeagueProviderAsync(Guid leagueProviderId)
    {
        var leagueProvider = await _leagueProviderRepository.GetByIdAsync(leagueProviderId);
        if (leagueProvider == null)
        {
            return Result<LeagueProvider>.Failure($"League-provider mapping with ID {leagueProviderId} not found");
        }

        // Deactivate all other providers for this league
        var allProvidersForLeague = await _leagueProviderRepository.GetByLeagueIdAsync(leagueProvider.LeagueId);
        foreach (var provider in allProvidersForLeague.Where(p => p.Id != leagueProviderId))
        {
            provider.IsActive = false;
            await _leagueProviderRepository.UpdateAsync(provider);
        }

        // Activate this provider
        leagueProvider.IsActive = true;
        await _leagueProviderRepository.UpdateAsync(leagueProvider);

        return Result<LeagueProvider>.Success(leagueProvider);
    }
}
