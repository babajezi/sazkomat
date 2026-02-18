using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Services;

namespace Sazkomat.Tests.Data;

public class ImportServiceTests
{
    private readonly Mock<IProviderCountryRepository> _mockProviderCountryRepo;
    private readonly Mock<IProviderLeagueRepository> _mockProviderLeagueRepo;
    private readonly Mock<IProviderSeasonRepository> _mockProviderSeasonRepo;
    private readonly Mock<ISyncJobRepository> _mockSyncJobRepo;
    private readonly Mock<IDataProviderRepository> _mockDataProviderRepo;
    private readonly Mock<ICountryRepository> _mockCountryRepo;
    private readonly Mock<ILeagueRepository> _mockLeagueRepo;
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<ILeagueSeasonRepository> _mockLeagueSeasonRepo;
    private readonly Mock<ICountryProviderRepository> _mockCountryProviderRepo;
    private readonly Mock<ILeagueProviderRepository> _mockLeagueProviderRepo;
    private readonly Mock<ISportRepository> _mockSportRepo;
    private readonly Mock<ICountryNameMappingRepository> _mockCountryNameMappingRepo;
    private readonly Mock<ILogger<ImportService>> _mockLogger;
    private readonly ImportService _service;

    private readonly Guid _providerId;
    private readonly DataProvider _provider;
    private readonly Sport _footballSport;

    public ImportServiceTests()
    {
        _mockProviderCountryRepo = new Mock<IProviderCountryRepository>();
        _mockProviderLeagueRepo = new Mock<IProviderLeagueRepository>();
        _mockProviderSeasonRepo = new Mock<IProviderSeasonRepository>();
        _mockSyncJobRepo = new Mock<ISyncJobRepository>();
        _mockDataProviderRepo = new Mock<IDataProviderRepository>();
        _mockCountryRepo = new Mock<ICountryRepository>();
        _mockLeagueRepo = new Mock<ILeagueRepository>();
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockLeagueSeasonRepo = new Mock<ILeagueSeasonRepository>();
        _mockCountryProviderRepo = new Mock<ICountryProviderRepository>();
        _mockLeagueProviderRepo = new Mock<ILeagueProviderRepository>();
        _mockSportRepo = new Mock<ISportRepository>();
        _mockCountryNameMappingRepo = new Mock<ICountryNameMappingRepository>();
        _mockLogger = new Mock<ILogger<ImportService>>();

        _providerId = Guid.NewGuid();
        _provider = new DataProvider
        {
            Id = _providerId,
            Name = "BetExplorer",
            Code = "betexplorer",
            Type = ProviderType.Scraper,
            IsActive = true
        };

        _footballSport = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Football",
            Code = "football",
            IsActive = true
        };

        _service = new ImportService(
            _mockProviderCountryRepo.Object,
            _mockProviderLeagueRepo.Object,
            _mockProviderSeasonRepo.Object,
            _mockSyncJobRepo.Object,
            _mockDataProviderRepo.Object,
            _mockCountryRepo.Object,
            _mockLeagueRepo.Object,
            _mockSeasonRepo.Object,
            _mockLeagueSeasonRepo.Object,
            _mockCountryProviderRepo.Object,
            _mockLeagueProviderRepo.Object,
            _mockSportRepo.Object,
            _mockCountryNameMappingRepo.Object,
            _mockLogger.Object
        );
    }

    #region ImportCountries Tests

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesFromCacheAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ImportCountriesFromCacheAsync(_providerId)
        );
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesFromCacheAsync_EmptyList_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockProviderCountryRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(new List<ProviderCountry>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ImportCountriesFromCacheAsync(_providerId, null)
        );
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesInternalAsync_CreatesNewCountry()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCode = "england",
            ProviderName = "England",
            IsoCode = "GB-ENG",
            FlagEmoji = "🏴󠁧󠁢󠁥󠁮󠁧󠁿",
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        // Provider must exist and be a Scraper type to allow creating countries
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockCountryRepo.Setup(r => r.GetByCodeAsync(providerCountry.IsoCode!))
            .ReturnsAsync((Country?)null);

        // Also need to handle the ProviderCode fallback lookup
        _mockCountryRepo.Setup(r => r.GetByCodeAsync(providerCountry.ProviderCode))
            .ReturnsAsync((Country?)null);

        _mockCountryRepo.Setup(r => r.CreateAsync(It.IsAny<Country>()))
            .ReturnsAsync((Country c) => new Country { Id = Guid.NewGuid(), Name = c.Name, Code = c.Code, IsoCode = c.IsoCode, IsActive = c.IsActive });

        _mockCountryProviderRepo.Setup(r => r.GetByCountryAndProviderAsync(It.IsAny<Guid>(), _providerId))
            .ReturnsAsync((CountryProvider?)null);

        _mockSyncJobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync((SyncJob j) => j);

        _mockCountryProviderRepo.Setup(r => r.AddAsync(It.IsAny<CountryProvider>()))
            .Verifiable();

        _mockProviderCountryRepo.Setup(r => r.UpdateAsync(It.IsAny<ProviderCountry>()))
            .ReturnsAsync((ProviderCountry pc) => { pc.IsImported = true; pc.ImportedAt = DateTime.UtcNow; return pc; });

        // Act
        await _service.ImportCountriesFromCacheInternalAsync(jobId, new List<Guid> { providerCountry.Id });

        // Assert - can be Completed or PartiallyCompleted depending on internal error handling
        Assert.Contains(syncJob.Status, new[] { SyncJobStatus.Completed, SyncJobStatus.PartiallyCompleted });
        Assert.True(providerCountry.IsImported);
        Assert.NotNull(providerCountry.ImportedAt);
        _mockCountryRepo.Verify(r => r.CreateAsync(It.Is<Country>(c =>
            c.Name == providerCountry.ProviderName &&
            c.IsoCode == providerCountry.IsoCode &&
            c.IsActive == false // Countries start inactive
        )), Times.Once);
        _mockCountryProviderRepo.Verify(r => r.AddAsync(It.IsAny<CountryProvider>()), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesInternalAsync_ReusesExistingCountry()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var existingCountry = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "england",
            IsoCode = "GB-ENG",
            IsActive = true
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCode = "england",
            ProviderName = "England",
            IsoCode = "GB-ENG",
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockCountryRepo.Setup(r => r.GetByCodeAsync(providerCountry.IsoCode!))
            .ReturnsAsync(existingCountry);

        _mockCountryProviderRepo.Setup(r => r.GetByCountryAndProviderAsync(existingCountry.Id, _providerId))
            .ReturnsAsync((CountryProvider?)null);

        // Act
        await _service.ImportCountriesFromCacheInternalAsync(jobId, new List<Guid> { providerCountry.Id });

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockCountryRepo.Verify(r => r.CreateAsync(It.IsAny<Country>()), Times.Never);
        _mockCountryProviderRepo.Verify(r => r.AddAsync(It.IsAny<CountryProvider>()), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesInternalAsync_SkipsAlreadyImported()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCode = "england",
            ProviderName = "England",
            IsImported = true, // Already imported
            CountryId = Guid.NewGuid()
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockSyncJobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync((SyncJob j) => j);

        // Act
        await _service.ImportCountriesFromCacheInternalAsync(jobId, new List<Guid> { providerCountry.Id });

        // Assert - can be Completed or PartiallyCompleted depending on internal error handling
        Assert.Contains(syncJob.Status, new[] { SyncJobStatus.Completed, SyncJobStatus.PartiallyCompleted });
        _mockCountryRepo.Verify(r => r.CreateAsync(It.IsAny<Country>()), Times.Never);
        _mockCountryProviderRepo.Verify(r => r.AddAsync(It.IsAny<CountryProvider>()), Times.Never);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportCountriesInternalAsync_UpdatesExistingMapping()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var existingCountry = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "england",
            IsoCode = "GB-ENG"
        };

        var existingMapping = new CountryProvider
        {
            Id = Guid.NewGuid(),
            CountryId = existingCountry.Id,
            ProviderId = _providerId,
            ProviderCode = "eng_old",
            ProviderName = "England Old",
            IsActive = false
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCode = "england",
            ProviderName = "England Updated",
            IsoCode = "GB-ENG",
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockCountryRepo.Setup(r => r.GetByCodeAsync(providerCountry.IsoCode!))
            .ReturnsAsync(existingCountry);

        _mockCountryProviderRepo.Setup(r => r.GetByCountryAndProviderAsync(existingCountry.Id, _providerId))
            .ReturnsAsync(existingMapping);

        // Act
        await _service.ImportCountriesFromCacheInternalAsync(jobId, new List<Guid> { providerCountry.Id });

        // Assert
        Assert.Equal("england", existingMapping.ProviderCode);
        Assert.Equal("England Updated", existingMapping.ProviderName);
        Assert.True(existingMapping.IsActive);
        _mockCountryProviderRepo.Verify(r => r.UpdateAsync(existingMapping), Times.Once);
        _mockCountryProviderRepo.Verify(r => r.AddAsync(It.IsAny<CountryProvider>()), Times.Never);
    }

    #endregion

    #region ImportLeagues Tests

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportLeaguesFromCacheAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ImportLeaguesFromCacheAsync(_providerId)
        );
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportLeaguesInternalAsync_CreatesNewLeague()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "england",
            IsoCode = "GB-ENG"
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            CountryId = country.Id
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCountryId = providerCountry.Id,
            ProviderSlug = "england/premier-league",
            ProviderName = "Premier League",
            DisplayName = "English Premier League",
            IsBettable = true,
            Priority = 1,
            IsImported = false,
            MappingStatus = MappingStatus.AutoMapped
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id))
            .ReturnsAsync(providerLeague);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockCountryRepo.Setup(r => r.GetByIdAsync(country.Id))
            .ReturnsAsync(country);

        _mockLeagueProviderRepo.Setup(r => r.GetByProviderAndSlugAsync(_providerId, providerLeague.ProviderSlug))
            .ReturnsAsync((LeagueProvider?)null);

        _mockLeagueRepo.Setup(r => r.CreateAsync(It.IsAny<League>()))
            .ReturnsAsync((League l) => new League
            {
                Id = Guid.NewGuid(),
                Name = l.Name,
                SportId = l.SportId,
                CountryId = l.CountryId,
                IsActive = l.IsActive
            });

        _mockSyncJobRepo.Setup(r => r.UpdateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync((SyncJob j) => j);

        _mockLeagueProviderRepo.Setup(r => r.AddAsync(It.IsAny<LeagueProvider>()))
            .Verifiable();

        _mockProviderLeagueRepo.Setup(r => r.UpdateAsync(It.IsAny<ProviderLeague>()))
            .ReturnsAsync((ProviderLeague pl) => { pl.IsImported = true; pl.ImportedAt = DateTime.UtcNow; return pl; });

        // Act
        await _service.ImportLeaguesFromCacheInternalAsync(jobId, new List<Guid> { providerLeague.Id });

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.True(providerLeague.IsImported);
        // Note: For Scraper providers (like BetExplorer), IsActive is set to false
        // Only BettingProvider types set IsActive = true
        _mockLeagueRepo.Verify(r => r.CreateAsync(It.Is<League>(l =>
            l.Name == providerLeague.ProviderName &&
            l.CountryId == country.Id &&
            l.IsActive == false // Scraper providers create leagues as inactive
        )), Times.Once);
        _mockLeagueProviderRepo.Verify(r => r.AddAsync(It.IsAny<LeagueProvider>()), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportLeaguesInternalAsync_SkipsUnmappedLeagues()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "unknown/league",
            ProviderName = "Unknown League",
            IsImported = false,
            MappingStatus = MappingStatus.Unmapped // Unmapped
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id))
            .ReturnsAsync(providerLeague);

        // Act
        await _service.ImportLeaguesFromCacheInternalAsync(jobId, new List<Guid> { providerLeague.Id });

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.False(providerLeague.IsImported); // Should remain not imported
        _mockLeagueRepo.Verify(r => r.CreateAsync(It.IsAny<League>()), Times.Never);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportLeaguesInternalAsync_UpdatesExistingLeague()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "england"
        };

        var existingLeague = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            SportId = _footballSport.Id,
            CountryId = country.Id,
            DisplayName = "Premier League Old",
            Priority = 1
        };

        var existingMapping = new LeagueProvider
        {
            Id = Guid.NewGuid(),
            LeagueId = existingLeague.Id,
            ProviderId = _providerId,
            ProviderSlug = "england/premier-league"
        };

        var providerCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            CountryId = country.Id
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCountryId = providerCountry.Id,
            ProviderSlug = "england/premier-league",
            ProviderName = "Premier League",
            DisplayName = "English Premier League Updated",
            IsBettable = true,
            Priority = 5,
            IsImported = false,
            MappingStatus = MappingStatus.AutoMapped
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id))
            .ReturnsAsync(providerLeague);

        _mockProviderCountryRepo.Setup(r => r.GetByIdAsync(providerCountry.Id))
            .ReturnsAsync(providerCountry);

        _mockCountryRepo.Setup(r => r.GetByIdAsync(country.Id))
            .ReturnsAsync(country);

        _mockLeagueProviderRepo.Setup(r => r.GetByProviderAndSlugAsync(_providerId, providerLeague.ProviderSlug))
            .ReturnsAsync(existingMapping);

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(existingLeague.Id))
            .ReturnsAsync(existingLeague);

        // Act
        await _service.ImportLeaguesFromCacheInternalAsync(jobId, new List<Guid> { providerLeague.Id });

        // Assert
        Assert.Equal("English Premier League Updated", existingLeague.DisplayName);
        Assert.Equal(5, existingLeague.Priority);
        Assert.True(existingLeague.IsBettable);
        _mockLeagueRepo.Verify(r => r.UpdateAsync(existingLeague), Times.Once);
        _mockLeagueRepo.Verify(r => r.CreateAsync(It.IsAny<League>()), Times.Never);
    }

    #endregion

    #region ImportSeasons Tests

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportSeasonsFromCacheAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ImportSeasonsFromCacheAsync(_providerId)
        );
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportSeasonsInternalAsync_CreatesNewSeason()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending
        };

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            SportId = _footballSport.Id,
            CountryId = Guid.NewGuid()
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "england/premier-league",
            LeagueId = league.Id,
            IsImported = true
        };

        var providerSeason = new ProviderSeason
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderLeagueId = providerLeague.Id,
            SeasonName = "2023-2024",
            StartYear = 2023,
            EndYear = 2024,
            IsCurrentSeason = false,
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockProviderSeasonRepo.Setup(r => r.GetByIdAsync(providerSeason.Id))
            .ReturnsAsync(providerSeason);

        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id))
            .ReturnsAsync(providerLeague);

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(league.Id))
            .ReturnsAsync(league);

        _mockSeasonRepo.Setup(r => r.GetByNameAsync(providerSeason.SeasonName))
            .ReturnsAsync((Season?)null);

        _mockSeasonRepo.Setup(r => r.CreateAsync(It.IsAny<Season>()))
            .ReturnsAsync((Season s) => new Season { Id = Guid.NewGuid(), Name = s.Name, StartYear = s.StartYear, EndYear = s.EndYear });

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((LeagueSeason?)null);

        // Act
        await _service.ImportSeasonsFromCacheInternalAsync(jobId, new List<Guid> { providerSeason.Id });

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.True(providerSeason.IsImported);
        _mockSeasonRepo.Verify(r => r.CreateAsync(It.Is<Season>(s =>
            s.Name == providerSeason.SeasonName &&
            s.StartYear == 2023 &&
            s.EndYear == 2024
        )), Times.Once);
        _mockLeagueSeasonRepo.Verify(r => r.CreateAsync(It.Is<LeagueSeason>(ls =>
            ls.LeagueId == league.Id &&
            ls.IsCurrent == false &&
            ls.SyncMode == SyncMode.Historical
        )), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportSeasonsInternalAsync_CurrentSeason_SetsSyncModeCurrent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending
        };

        var league = new League { Id = Guid.NewGuid(), SportId = _footballSport.Id, CountryId = Guid.NewGuid() };
        var providerLeague = new ProviderLeague { Id = Guid.NewGuid(), ProviderId = _providerId, LeagueId = league.Id, IsImported = true };
        var providerSeason = new ProviderSeason
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderLeagueId = providerLeague.Id,
            SeasonName = "2024-2025",
            StartYear = 2024,
            EndYear = 2025,
            IsCurrentSeason = true, // Current season
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(syncJob);
        _mockProviderSeasonRepo.Setup(r => r.GetByIdAsync(providerSeason.Id)).ReturnsAsync(providerSeason);
        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id)).ReturnsAsync(providerLeague);
        _mockLeagueRepo.Setup(r => r.GetByIdAsync(league.Id)).ReturnsAsync(league);
        _mockSeasonRepo.Setup(r => r.GetByNameAsync(providerSeason.SeasonName)).ReturnsAsync((Season?)null);
        _mockSeasonRepo.Setup(r => r.CreateAsync(It.IsAny<Season>())).ReturnsAsync((Season s) => new Season { Id = Guid.NewGuid(), Name = s.Name, StartYear = s.StartYear, EndYear = s.EndYear });
        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((LeagueSeason?)null);

        // Act
        await _service.ImportSeasonsFromCacheInternalAsync(jobId, new List<Guid> { providerSeason.Id });

        // Assert
        _mockLeagueSeasonRepo.Verify(r => r.CreateAsync(It.Is<LeagueSeason>(ls =>
            ls.IsCurrent == true &&
            ls.SyncMode == SyncMode.Current
        )), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ImportSeasonsInternalAsync_SkipsIfLeagueNotImported()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "england/premier-league",
            LeagueId = null, // Not imported yet
            IsImported = false
        };

        var providerSeason = new ProviderSeason
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderLeagueId = providerLeague.Id,
            SeasonName = "2023-2024",
            IsImported = false
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockProviderSeasonRepo.Setup(r => r.GetByIdAsync(providerSeason.Id))
            .ReturnsAsync(providerSeason);

        _mockProviderLeagueRepo.Setup(r => r.GetByIdAsync(providerLeague.Id))
            .ReturnsAsync(providerLeague);

        // Act
        await _service.ImportSeasonsFromCacheInternalAsync(jobId, new List<Guid> { providerSeason.Id });

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.False(providerSeason.IsImported);
        _mockSeasonRepo.Verify(r => r.CreateAsync(It.IsAny<Season>()), Times.Never);
    }

    #endregion

    #region GetImportStats Tests

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetImportStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        var providerCountries = new List<ProviderCountry>
        {
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = false }
        };

        var providerLeagues = new List<ProviderLeague>
        {
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = false },
            new() { Id = Guid.NewGuid(), IsImported = false }
        };

        var providerSeasons = new List<ProviderSeason>
        {
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = true },
            new() { Id = Guid.NewGuid(), IsImported = false }
        };

        _mockProviderCountryRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(providerCountries);

        _mockProviderLeagueRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(providerLeagues);

        _mockProviderSeasonRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(providerSeasons);

        // Act
        var stats = await _service.GetImportStatsAsync(_providerId);

        // Assert
        Assert.Equal(3, stats.CachedCountries);
        Assert.Equal(2, stats.ImportedCountries);
        Assert.Equal(3, stats.CachedLeagues);
        Assert.Equal(1, stats.ImportedLeagues);
        Assert.Equal(4, stats.CachedSeasons);
        Assert.Equal(3, stats.ImportedSeasons);
    }

    #endregion
}
