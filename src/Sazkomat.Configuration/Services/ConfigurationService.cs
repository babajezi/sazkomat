using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;

namespace Sazkomat.Configuration.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ISportRepository _sportRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ICountryProviderRepository _countryProviderRepository;
    private readonly ILeagueProviderRepository _leagueProviderRepository;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        ISportRepository sportRepository,
        ICountryRepository countryRepository,
        ILeagueRepository leagueRepository,
        ICountryProviderRepository countryProviderRepository,
        ILeagueProviderRepository leagueProviderRepository,
        ILogger<ConfigurationService> logger)
    {
        _sportRepository = sportRepository;
        _countryRepository = countryRepository;
        _leagueRepository = leagueRepository;
        _countryProviderRepository = countryProviderRepository;
        _leagueProviderRepository = leagueProviderRepository;
        _logger = logger;
    }

    public async Task<Result<Sport>> UpdateSportAsync(Guid sportId, UpdateSportRequest request)
    {
        _logger.LogInformation("Updating sport {SportId} with IsActive={IsActive}", sportId, request.IsActive);

        var sport = await _sportRepository.GetByIdAsync(sportId);
        if (sport == null)
        {
            _logger.LogWarning("Failed to update sport {SportId}: Sport not found", sportId);
            return Result<Sport>.Failure($"Sport with ID {sportId} not found");
        }

        // Update only provided fields (partial update)
        if (request.IsActive.HasValue)
        {
            sport.IsActive = request.IsActive.Value;
        }

        var updatedSport = await _sportRepository.UpdateAsync(sport);
        _logger.LogInformation("Successfully updated sport {SportId} ({SportName}). IsActive={IsActive}",
            updatedSport.Id, updatedSport.Name, updatedSport.IsActive);

        return Result<Sport>.Success(updatedSport);
    }

    public async Task<Result<League>> CreateLeagueAsync(CreateLeagueRequest request)
    {
        _logger.LogInformation(
            "Creating league {LeagueName} for sport {SportId} in country {CountryId}",
            request.Name, request.SportId, request.CountryId);

        // Validate sport exists
        var sport = await _sportRepository.GetByIdAsync(request.SportId);
        if (sport == null)
        {
            _logger.LogWarning(
                "Failed to create league {LeagueName}: Sport {SportId} not found",
                request.Name, request.SportId);
            return Result<League>.Failure($"Sport with ID {request.SportId} not found");
        }

        // Validate country exists
        var country = await _countryRepository.GetByIdAsync(request.CountryId);
        if (country == null)
        {
            _logger.LogWarning(
                "Failed to create league {LeagueName}: Country {CountryId} not found",
                request.Name, request.CountryId);
            return Result<League>.Failure($"Country with ID {request.CountryId} not found");
        }

        // Check for duplicate league (same sport, country, name)
        var existingLeagues = await _leagueRepository.GetAllAsync(request.SportId, request.CountryId);
        if (existingLeagues.Any(l => l.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(
                "Failed to create league {LeagueName}: Duplicate league already exists for {SportName} in {CountryName}",
                request.Name, sport.Name, country.Name);
            return Result<League>.Failure($"League '{request.Name}' already exists for {sport.Name} in {country.Name}");
        }

        var league = new League
        {
            SportId = request.SportId,
            CountryId = request.CountryId,
            Name = request.Name,
            NameCs = request.NameCs,
            DisplayName = $"{request.Name} ({country.Name})",
            BetExplorerSlug = request.BetExplorerSlug,
            IsBettable = request.IsBettable,
            Priority = request.Priority,
            Notes = request.Notes,
            IsSyncEnabled = false // Default to disabled
        };

        var createdLeague = await _leagueRepository.CreateAsync(league);
        _logger.LogInformation(
            "Successfully created league {LeagueId} ({LeagueName}) for sport {SportName} in {CountryName}",
            createdLeague.Id, createdLeague.Name, sport.Name, country.Name);

        return Result<League>.Success(createdLeague);
    }

    public async Task<Result<League>> UpdateLeagueAsync(Guid leagueId, UpdateLeagueRequest request)
    {
        _logger.LogInformation("Updating league {LeagueId}", leagueId);

        var league = await _leagueRepository.GetByIdAsync(leagueId);
        if (league == null)
        {
            _logger.LogWarning("Failed to update league {LeagueId}: League not found", leagueId);
            return Result<League>.Failure($"League with ID {leagueId} not found");
        }

        var originalName = league.Name;

        // Update only provided fields (partial update)
        if (request.Name != null)
        {
            league.Name = request.Name;
            league.DisplayName = $"{request.Name} ({league.Country.Name})";
        }

        if (request.NameCs != null)
        {
            league.NameCs = request.NameCs;
        }

        if (request.BetExplorerSlug != null)
        {
            league.BetExplorerSlug = request.BetExplorerSlug;
        }

        if (request.IsSyncEnabled.HasValue)
        {
            league.IsSyncEnabled = request.IsSyncEnabled.Value;
        }

        if (request.IsBettable.HasValue)
        {
            league.IsBettable = request.IsBettable.Value;
        }

        if (request.IsActive.HasValue)
        {
            league.IsActive = request.IsActive.Value;
        }

        if (request.Priority.HasValue)
        {
            league.Priority = request.Priority.Value;
        }

        if (request.Notes != null)
        {
            league.Notes = request.Notes;
        }

        var updatedLeague = await _leagueRepository.UpdateAsync(league);
        _logger.LogInformation(
            "Successfully updated league {LeagueId} ({OriginalName} → {NewName}). IsSyncEnabled={IsSyncEnabled}, IsActive={IsActive}",
            updatedLeague.Id, originalName, updatedLeague.Name, updatedLeague.IsSyncEnabled, updatedLeague.IsActive);

        return Result<League>.Success(updatedLeague);
    }

    public async Task<Result> DeleteLeagueAsync(Guid leagueId)
    {
        _logger.LogInformation("Deleting league {LeagueId}", leagueId);

        var league = await _leagueRepository.GetByIdAsync(leagueId);
        if (league == null)
        {
            _logger.LogWarning("Failed to delete league {LeagueId}: League not found", leagueId);
            return Result.Failure($"League with ID {leagueId} not found");
        }

        var leagueName = league.Name;
        await _leagueRepository.DeleteAsync(leagueId);
        _logger.LogInformation("Successfully deleted league {LeagueId} ({LeagueName})", leagueId, leagueName);

        return Result.Success();
    }

    public async Task<Result<Country>> CreateCountryAsync(CreateCountryRequest request)
    {
        _logger.LogInformation("Creating country {CountryName} with code {CountryCode}", request.Name, request.Code);

        // Check for duplicate country code
        var existingCountries = await _countryRepository.GetAllAsync();
        if (existingCountries.Any(c => c.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(
                "Failed to create country {CountryName}: Country code {CountryCode} already exists",
                request.Name, request.Code);
            return Result<Country>.Failure($"Country with code '{request.Code}' already exists");
        }

        var country = new Country
        {
            Name = request.Name,
            Code = request.Code.ToUpper(),
            FlagEmoji = request.FlagEmoji
        };

        var createdCountry = await _countryRepository.CreateAsync(country);
        _logger.LogInformation(
            "Successfully created country {CountryId} ({CountryName}, {CountryCode})",
            createdCountry.Id, createdCountry.Name, createdCountry.Code);

        return Result<Country>.Success(createdCountry);
    }

    public async Task<Result<Country>> UpdateCountryAsync(Guid countryId, UpdateCountryRequest request)
    {
        _logger.LogInformation("Updating country {CountryId}", countryId);

        var country = await _countryRepository.GetByIdAsync(countryId);
        if (country == null)
        {
            _logger.LogWarning("Failed to update country {CountryId}: Country not found", countryId);
            return Result<Country>.Failure($"Country with ID {countryId} not found");
        }

        var originalName = country.Name;

        // Update only provided fields (partial update)
        if (request.Name != null)
        {
            country.Name = request.Name;
        }

        if (request.NameCs != null)
        {
            country.NameCs = request.NameCs;
        }

        if (request.Code != null)
        {
            // Check for duplicate code
            var existingCountries = await _countryRepository.GetAllAsync();
            if (existingCountries.Any(c => c.Id != countryId && c.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning(
                    "Failed to update country {CountryId}: Country code {CountryCode} already exists",
                    countryId, request.Code);
                return Result<Country>.Failure($"Country with code '{request.Code}' already exists");
            }
            country.Code = request.Code.ToUpper();
        }

        if (request.FlagEmoji != null)
        {
            country.FlagEmoji = request.FlagEmoji;
        }

        if (request.IsActive.HasValue)
        {
            country.IsActive = request.IsActive.Value;
        }

        var updatedCountry = await _countryRepository.UpdateAsync(country);
        _logger.LogInformation(
            "Successfully updated country {CountryId} ({OriginalName} → {NewName}). Code={CountryCode}, IsActive={IsActive}",
            updatedCountry.Id, originalName, updatedCountry.Name, updatedCountry.Code, updatedCountry.IsActive);

        return Result<Country>.Success(updatedCountry);
    }

    public async Task<Result> DeleteCountryAsync(Guid countryId)
    {
        _logger.LogInformation("Deleting country {CountryId}", countryId);

        var country = await _countryRepository.GetByIdAsync(countryId);
        if (country == null)
        {
            _logger.LogWarning("Failed to delete country {CountryId}: Country not found", countryId);
            return Result.Failure($"Country with ID {countryId} not found");
        }

        // Check if country has any leagues
        var leagues = await _leagueRepository.GetAllAsync(null, countryId, null);
        if (leagues.Any())
        {
            _logger.LogWarning(
                "Failed to delete country {CountryId} ({CountryName}): Has {LeagueCount} associated leagues",
                countryId, country.Name, leagues.Count);
            return Result.Failure($"Cannot delete country '{country.Name}' because it has {leagues.Count} associated leagues");
        }

        var countryName = country.Name;
        await _countryRepository.DeleteAsync(countryId);
        _logger.LogInformation("Successfully deleted country {CountryId} ({CountryName})", countryId, countryName);

        return Result.Success();
    }

    public async Task<Result<CountryProvider>> ToggleCountryProviderSyncAsync(Guid countryId, Guid providerId, bool isActive)
    {
        _logger.LogInformation(
            "Toggling country provider sync for country {CountryId}, provider {ProviderId} to {IsActive}",
            countryId, providerId, isActive);

        // Get country to validate isActive
        var country = await _countryRepository.GetByIdAsync(countryId);
        if (country == null)
        {
            _logger.LogWarning(
                "Failed to toggle country provider sync: Country {CountryId} not found",
                countryId);
            return Result<CountryProvider>.Failure($"Country with ID {countryId} not found");
        }

        // Validate: Cannot enable sync if country is not active
        if (isActive && !country.IsActive)
        {
            _logger.LogWarning(
                "Failed to enable sync for country {CountryId} ({CountryName}): Country is inactive",
                countryId, country.Name);
            return Result<CountryProvider>.Failure($"Cannot enable synchronization for inactive country '{country.Name}'. Please activate the country first.");
        }

        // Get country provider
        var countryProvider = await _countryProviderRepository.GetByCountryAndProviderAsync(countryId, providerId);
        if (countryProvider == null)
        {
            _logger.LogWarning(
                "Failed to toggle country provider sync: Mapping not found for country {CountryId} and provider {ProviderId}",
                countryId, providerId);
            return Result<CountryProvider>.Failure($"Country provider mapping not found for country ID {countryId} and provider ID {providerId}");
        }

        // Update isActive status
        countryProvider.IsActive = isActive;
        await _countryProviderRepository.UpdateAsync(countryProvider);

        _logger.LogInformation(
            "Successfully toggled country provider sync for {CountryName} (provider {ProviderId}) to {IsActive}",
            country.Name, providerId, isActive);

        return Result<CountryProvider>.Success(countryProvider);
    }

    public async Task<Result<LeagueProvider>> ToggleLeagueProviderSyncAsync(Guid leagueId, Guid providerId, bool isActive)
    {
        _logger.LogInformation(
            "Toggling league provider sync for league {LeagueId}, provider {ProviderId} to {IsActive}",
            leagueId, providerId, isActive);

        // Get league to validate isActive
        var league = await _leagueRepository.GetByIdAsync(leagueId);
        if (league == null)
        {
            _logger.LogWarning(
                "Failed to toggle league provider sync: League {LeagueId} not found",
                leagueId);
            return Result<LeagueProvider>.Failure($"League with ID {leagueId} not found");
        }

        // Get country to validate isActive
        var country = await _countryRepository.GetByIdAsync(league.CountryId);
        if (country == null)
        {
            _logger.LogWarning(
                "Failed to toggle league provider sync: Country {CountryId} not found for league {LeagueId}",
                league.CountryId, leagueId);
            return Result<LeagueProvider>.Failure($"Country with ID {league.CountryId} not found");
        }

        // Validate: Cannot enable sync if league or country is not active
        if (isActive && !league.IsActive)
        {
            _logger.LogWarning(
                "Failed to enable sync for league {LeagueId} ({LeagueName}): League is inactive",
                leagueId, league.DisplayName);
            return Result<LeagueProvider>.Failure($"Cannot enable synchronization for inactive league '{league.DisplayName}'. Please activate the league first.");
        }

        if (isActive && !country.IsActive)
        {
            _logger.LogWarning(
                "Failed to enable sync for league {LeagueId} ({LeagueName}): Country {CountryName} is inactive",
                leagueId, league.DisplayName, country.Name);
            return Result<LeagueProvider>.Failure($"Cannot enable synchronization for league '{league.DisplayName}' because its country '{country.Name}' is inactive. Please activate the country first.");
        }

        // Get league provider
        var leagueProvider = await _leagueProviderRepository.GetByLeagueAndProviderAsync(leagueId, providerId);
        if (leagueProvider == null)
        {
            _logger.LogWarning(
                "Failed to toggle league provider sync: Mapping not found for league {LeagueId} and provider {ProviderId}",
                leagueId, providerId);
            return Result<LeagueProvider>.Failure($"League provider mapping not found for league ID {leagueId} and provider ID {providerId}");
        }

        // Update isActive status
        leagueProvider.IsActive = isActive;
        await _leagueProviderRepository.UpdateAsync(leagueProvider);

        _logger.LogInformation(
            "Successfully toggled league provider sync for {LeagueName} (provider {ProviderId}) to {IsActive}",
            league.DisplayName, providerId, isActive);

        return Result<LeagueProvider>.Success(leagueProvider);
    }
}
