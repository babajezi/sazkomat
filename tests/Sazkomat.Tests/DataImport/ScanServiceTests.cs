using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Tests.DataImport;

public class ScanServiceTests
{
    private readonly Mock<IProviderCountryRepository> _mockProviderCountryRepo;
    private readonly Mock<IProviderLeagueRepository> _mockProviderLeagueRepo;
    private readonly Mock<IProviderSeasonRepository> _mockProviderSeasonRepo;
    private readonly Mock<ISyncJobRepository> _mockSyncJobRepo;
    private readonly Mock<IDataProviderRepository> _mockDataProviderRepo;
    private readonly Mock<ISportRepository> _mockSportRepo;
    private readonly Mock<ICountryRepository> _mockCountryRepo;
    private readonly Mock<ICountryProviderRepository> _mockCountryProviderRepo;
    private readonly Mock<ILeagueRepository> _mockLeagueRepo;
    private readonly Mock<ICountryScraper> _mockCountryScraper;
    private readonly Mock<ILeagueMetadataScraper> _mockLeagueScraper;
    private readonly Mock<ISeasonScraper> _mockSeasonScraper;
    private readonly Mock<IBetExplorerEnrichmentService> _mockEnrichmentService;
    private readonly Mock<ILogger<ScanService>> _mockLogger;
    private readonly ScanService _service;

    private readonly Guid _providerId;
    private readonly DataProvider _provider;
    private readonly Sport _footballSport;

    public ScanServiceTests()
    {
        _mockProviderCountryRepo = new Mock<IProviderCountryRepository>();
        _mockProviderLeagueRepo = new Mock<IProviderLeagueRepository>();
        _mockProviderSeasonRepo = new Mock<IProviderSeasonRepository>();
        _mockSyncJobRepo = new Mock<ISyncJobRepository>();
        _mockDataProviderRepo = new Mock<IDataProviderRepository>();
        _mockSportRepo = new Mock<ISportRepository>();
        _mockCountryRepo = new Mock<ICountryRepository>();
        _mockCountryProviderRepo = new Mock<ICountryProviderRepository>();
        _mockLeagueRepo = new Mock<ILeagueRepository>();
        _mockCountryScraper = new Mock<ICountryScraper>();
        _mockLeagueScraper = new Mock<ILeagueMetadataScraper>();
        _mockSeasonScraper = new Mock<ISeasonScraper>();
        _mockEnrichmentService = new Mock<IBetExplorerEnrichmentService>();
        _mockLogger = new Mock<ILogger<ScanService>>();

        _providerId = Guid.NewGuid();
        _provider = new DataProvider
        {
            Id = _providerId,
            Name = "BetExplorer",
            Code = "betexplorer",
            Type = ProviderType.Scraper,
            BaseUrl = "https://www.betexplorer.com",
            IsActive = true
        };

        _footballSport = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Football",
            Code = "football",
            IsActive = true
        };

        _service = new ScanService(
            _mockProviderCountryRepo.Object,
            _mockProviderLeagueRepo.Object,
            _mockProviderSeasonRepo.Object,
            _mockSyncJobRepo.Object,
            _mockDataProviderRepo.Object,
            _mockSportRepo.Object,
            _mockCountryRepo.Object,
            _mockCountryProviderRepo.Object,
            _mockLeagueRepo.Object,
            new[] { _mockCountryScraper.Object },
            new[] { _mockLeagueScraper.Object },
            new[] { _mockSeasonScraper.Object },
            _mockEnrichmentService.Object,
            _mockLogger.Object
        );
    }

    #region ScanCountries Tests

    [Fact]
    public async Task ScanCountriesAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ScanCountriesAsync(_providerId)
        );
    }

    [Fact]
    public async Task ScanCountriesAsync_ValidProvider_CreatesSyncJob()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        var createdJob = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        _mockSyncJobRepo.Setup(r => r.CreateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync(createdJob);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockCountryScraper.Setup(s => s.CanHandle(_provider))
            .Returns(true);

        _mockCountryScraper.Setup(s => s.ScrapeCountriesAsync(_footballSport))
            .ReturnsAsync(new List<CountryMetadata>());

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(createdJob.Id))
            .ReturnsAsync(createdJob);

        // Act
        var jobId = await _service.ScanCountriesAsync(_providerId);

        // Assert
        Assert.Equal(createdJob.Id, jobId);
        _mockSyncJobRepo.Verify(r => r.CreateAsync(It.Is<SyncJob>(j =>
            j.ProviderId == _providerId &&
            j.Type == SyncJobType.Scan &&
            j.EntityType == SyncEntityType.Countries
        )), Times.Once);
    }

    [Fact]
    public async Task ScanCountriesInternalAsync_NoScraperAvailable_FailsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockCountryScraper.Setup(s => s.CanHandle(_provider))
            .Returns(false); // No scraper can handle this provider

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ScanCountriesInternalAsync(_providerId, jobId)
        );

        // Verify job was marked as failed
        Assert.Equal(SyncJobStatus.Failed, syncJob.Status);
        _mockSyncJobRepo.Verify(r => r.UpdateAsync(It.Is<SyncJob>(j =>
            j.Status == SyncJobStatus.Failed &&
            j.ErrorMessage != null
        )), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ScanCountriesInternalAsync_ScrapesAndCachesCountries()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var scrapedCountries = new List<CountryMetadata>
        {
            new() { Code = "england", Name = "England", IsoCode = "GB-ENG", FlagEmoji = "🏴󠁧󠁢󠁥󠁮󠁧󠁿" },
            new() { Code = "spain", Name = "Spain", IsoCode = "ES", FlagEmoji = "🇪🇸" }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockCountryScraper.Setup(s => s.CanHandle(_provider))
            .Returns(true);

        _mockCountryScraper.Setup(s => s.ScrapeCountriesAsync(_footballSport))
            .ReturnsAsync(scrapedCountries);

        _mockProviderCountryRepo.Setup(r => r.GetByProviderCodeAsync(_providerId, It.IsAny<string>()))
            .ReturnsAsync((ProviderCountry?)null); // All are new

        // Act
        await _service.ScanCountriesInternalAsync(_providerId, jobId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.NotNull(syncJob.CompletedAt);
        _mockProviderCountryRepo.Verify(r => r.CreateAsync(It.IsAny<ProviderCountry>()), Times.Exactly(2));
        _mockSyncJobRepo.Verify(r => r.UpdateAsync(It.Is<SyncJob>(j =>
            j.Status == SyncJobStatus.Completed
        )), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ScanCountriesInternalAsync_UpdatesExistingCountries()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending
        };

        var scrapedCountries = new List<CountryMetadata>
        {
            new() { Code = "england", Name = "England Updated", IsoCode = "GB-ENG", FlagEmoji = "🏴󠁧󠁢󠁥󠁮󠁧󠁿" }
        };

        var existingCountry = new ProviderCountry
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderCode = "england",
            ProviderName = "England",
            IsoCode = "GB-ENG"
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockCountryScraper.Setup(s => s.CanHandle(_provider))
            .Returns(true);

        _mockCountryScraper.Setup(s => s.ScrapeCountriesAsync(_footballSport))
            .ReturnsAsync(scrapedCountries);

        _mockProviderCountryRepo.Setup(r => r.GetByProviderCodeAsync(_providerId, "england"))
            .ReturnsAsync(existingCountry);

        // Act
        await _service.ScanCountriesInternalAsync(_providerId, jobId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.Equal("England Updated", existingCountry.ProviderName);
        _mockProviderCountryRepo.Verify(r => r.UpdateAsync(existingCountry), Times.Once);
        _mockProviderCountryRepo.Verify(r => r.CreateAsync(It.IsAny<ProviderCountry>()), Times.Never);
    }

    #endregion

    #region ScanLeagues Tests

    [Fact]
    public async Task ScanLeaguesAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ScanLeaguesAsync(_providerId)
        );
    }

    [Fact]
    public async Task ScanLeaguesAsync_ValidProvider_CreatesSyncJob()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        var createdJob = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        _mockSyncJobRepo.Setup(r => r.CreateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync(createdJob);

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(createdJob.Id))
            .ReturnsAsync(createdJob);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockLeagueScraper.Setup(s => s.CanHandle(_provider))
            .Returns(true);

        _mockProviderCountryRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(new List<ProviderCountry>());

        // Act
        var jobId = await _service.ScanLeaguesAsync(_providerId);

        // Assert
        Assert.Equal(createdJob.Id, jobId);
        _mockSyncJobRepo.Verify(r => r.CreateAsync(It.Is<SyncJob>(j =>
            j.ProviderId == _providerId &&
            j.Type == SyncJobType.Scan &&
            j.EntityType == SyncEntityType.Leagues
        )), Times.Once);
    }

    [Fact]
    public async Task ScanLeaguesInternalAsync_BettingProvider_UsesCountryProviderMapping()
    {
        // Arrange
        var bettingProvider = new DataProvider
        {
            Id = Guid.NewGuid(),
            Name = "Betano",
            Code = "betano",
            Type = ProviderType.BettingProvider,
            BaseUrl = "https://www.betano.cz",
            IsActive = true
        };

        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = bettingProvider.Id,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "GB-ENG",
            IsActive = true
        };

        var countryProvider = new CountryProvider
        {
            Id = Guid.NewGuid(),
            CountryId = country.Id,
            ProviderId = bettingProvider.Id,
            ProviderCountryCode = "england",
            IsActive = true,
            Country = country
        };

        var scrapedLeagues = new List<LeagueMetadata>
        {
            new() { Name = "Premier League", Slug = "england/premier-league", DisplayName = "Premier League (England)", Priority = 1, IsBettable = true }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(bettingProvider.Id))
            .ReturnsAsync(bettingProvider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockLeagueScraper.Setup(s => s.CanHandle(bettingProvider))
            .Returns(true);

        _mockCountryProviderRepo.Setup(r => r.GetByProviderIdAsync(bettingProvider.Id))
            .ReturnsAsync(new List<CountryProvider> { countryProvider });

        _mockLeagueScraper.Setup(s => s.ScrapeLeaguesAsync(_footballSport, country))
            .ReturnsAsync(scrapedLeagues);

        _mockEnrichmentService.Setup(s => s.EnrichLeagueAsync(It.IsAny<LeagueMetadata>(), country, bettingProvider.Code))
            .ReturnsAsync((LeagueMetadata league, Country c, string code) => league);

        _mockProviderLeagueRepo.Setup(r => r.GetByProviderSlugAsync(bettingProvider.Id, It.IsAny<string>()))
            .ReturnsAsync((ProviderLeague?)null);

        // Act
        await _service.ScanLeaguesInternalAsync(bettingProvider.Id, new List<Guid>(), jobId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockEnrichmentService.Verify(s => s.EnrichLeagueAsync(It.IsAny<LeagueMetadata>(), country, bettingProvider.Code), Times.Once);
        _mockProviderLeagueRepo.Verify(r => r.CreateAsync(It.IsAny<ProviderLeague>()), Times.Once);
    }

    [Fact]
    public async Task ScanLeaguesAsync_BettingProvider_AutoActivatesCountryAndCreatesMapping()
    {
        // Arrange
        var bettingProvider = new DataProvider
        {
            Id = Guid.NewGuid(),
            Name = "Betano",
            Code = "betano",
            Type = ProviderType.BettingProvider,
            BaseUrl = "https://www.betano.cz",
            IsActive = true
        };

        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = bettingProvider.Id,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending
        };

        // CRITICAL: Country starts INACTIVE
        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "Czech Republic",
            Code = "CZ",
            IsActive = false  // Start INACTIVE - this is the key test scenario
        };

        var countryProvider = new CountryProvider
        {
            Id = Guid.NewGuid(),
            CountryId = country.Id,
            ProviderId = bettingProvider.Id,
            ProviderCountryCode = "czech-republic",
            IsActive = true,
            Country = country
        };

        var scrapedLeagues = new List<LeagueMetadata>
        {
            new() { Name = "Czech Liga", Slug = "czech-republic/1-liga", DisplayName = "Czech Liga", Priority = 1, IsBettable = true }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(bettingProvider.Id))
            .ReturnsAsync(bettingProvider);

        _mockSportRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Sport> { _footballSport });

        _mockLeagueScraper.Setup(s => s.CanHandle(bettingProvider))
            .Returns(true);

        _mockCountryProviderRepo.Setup(r => r.GetByProviderIdAsync(bettingProvider.Id))
            .ReturnsAsync(new List<CountryProvider> { countryProvider });

        // CRITICAL: No mapping exists initially
        _mockCountryProviderRepo.Setup(r => r.GetByCountryAndProviderAsync(country.Id, bettingProvider.Id))
            .ReturnsAsync((CountryProvider?)null);

        _mockLeagueScraper.Setup(s => s.ScrapeLeaguesAsync(_footballSport, country))
            .ReturnsAsync(scrapedLeagues);

        _mockEnrichmentService.Setup(s => s.EnrichLeagueAsync(It.IsAny<LeagueMetadata>(), country, bettingProvider.Code))
            .ReturnsAsync((LeagueMetadata league, Country c, string code) => league);

        _mockProviderLeagueRepo.Setup(r => r.GetByProviderSlugAsync(bettingProvider.Id, It.IsAny<string>()))
            .ReturnsAsync((ProviderLeague?)null);

        // Act
        await _service.ScanLeaguesInternalAsync(bettingProvider.Id, new List<Guid>(), jobId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);

        // Verify country was activated
        _mockCountryRepo.Verify(r => r.UpdateAsync(It.Is<Country>(c =>
            c.Id == country.Id && c.IsActive == true)), Times.Once);

        // Verify CountryProvider mapping was created
        _mockCountryProviderRepo.Verify(r => r.AddAsync(It.Is<CountryProvider>(cp =>
            cp.CountryId == country.Id &&
            cp.ProviderId == bettingProvider.Id &&
            cp.ProviderCode == country.Code &&
            cp.ProviderName == country.Name &&
            cp.IsActive == true)), Times.Once);
    }

    #endregion

    #region ScanSeasons Tests

    [Fact]
    public async Task ScanSeasonsAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ScanSeasonsAsync(_providerId)
        );
    }

    [Fact]
    public async Task ScanSeasonsInternalAsync_ScrapesAndCachesSeasons()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Seasons,
            Status = SyncJobStatus.Pending
        };

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            BetExplorerSlug = "england/premier-league",
            SportId = _footballSport.Id,
            CountryId = Guid.NewGuid(),
            IsSyncEnabled = true
        };

        var providerLeague = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "england/premier-league",
            ProviderName = "Premier League",
            LeagueId = league.Id
        };

        var scrapedSeasons = new List<string> { "2023-2024", "2022-2023", "2021-2022" };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSeasonScraper.Setup(s => s.CanHandle(_provider))
            .Returns(true);

        _mockProviderLeagueRepo.Setup(r => r.GetByProviderIdAsync(_providerId))
            .ReturnsAsync(new List<ProviderLeague> { providerLeague });

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(league.Id))
            .ReturnsAsync(league);

        _mockSeasonScraper.Setup(s => s.ScrapeAvailableSeasonsAsync(league))
            .ReturnsAsync(scrapedSeasons);

        _mockProviderSeasonRepo.Setup(r => r.GetBySeasonNameAsync(providerLeague.Id, It.IsAny<string>()))
            .ReturnsAsync((ProviderSeason?)null); // All are new

        // Act
        await _service.ScanSeasonsInternalAsync(_providerId, new List<Guid>(), jobId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockProviderSeasonRepo.Verify(r => r.CreateAsync(It.IsAny<ProviderSeason>()), Times.Exactly(3));
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public async Task GetUnimportedCountriesAsync_ReturnsUnimported()
    {
        // Arrange
        var unimportedCountries = new List<ProviderCountry>
        {
            new() { Id = Guid.NewGuid(), ProviderId = _providerId, ProviderCode = "england", IsImported = false }
        };

        _mockProviderCountryRepo.Setup(r => r.GetUnimportedAsync(_providerId))
            .ReturnsAsync(unimportedCountries);

        // Act
        var result = await _service.GetUnimportedCountriesAsync(_providerId);

        // Assert
        Assert.Single(result);
        Assert.False(result[0].IsImported);
    }

    [Fact]
    public async Task GetUnimportedLeaguesAsync_ReturnsUnimported()
    {
        // Arrange
        var unimportedLeagues = new List<ProviderLeague>
        {
            new() { Id = Guid.NewGuid(), ProviderId = _providerId, ProviderSlug = "england/premier-league", IsImported = false }
        };

        _mockProviderLeagueRepo.Setup(r => r.GetUnimportedAsync(_providerId))
            .ReturnsAsync(unimportedLeagues);

        // Act
        var result = await _service.GetUnimportedLeaguesAsync(_providerId);

        // Assert
        Assert.Single(result);
        Assert.False(result[0].IsImported);
    }

    #endregion
}
