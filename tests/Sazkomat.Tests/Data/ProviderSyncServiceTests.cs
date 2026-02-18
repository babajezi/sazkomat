using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Scrapers;
using Sazkomat.Data.Services;
using Sazkomat.Data.Validators;

namespace Sazkomat.Tests.Data;

public class ProviderSyncServiceTests
{
    private readonly Mock<IDataProviderRepository> _mockProviderRepo;
    private readonly Mock<ISportRepository> _mockSportRepo;
    private readonly Mock<ICountryRepository> _mockCountryRepo;
    private readonly Mock<ILeagueRepository> _mockLeagueRepo;
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<ILeagueSeasonRepository> _mockLeagueSeasonRepo;
    private readonly Mock<ICountryProviderRepository> _mockCountryProviderRepo;
    private readonly Mock<ILeagueProviderRepository> _mockLeagueProviderRepo;
    private readonly Mock<ICountryScraper> _mockCountryScraper;
    private readonly Mock<ILeagueMetadataScraper> _mockLeagueScraper;
    private readonly Mock<ISeasonScraper> _mockSeasonScraper;
    private readonly Mock<ISeasonSyncService> _mockSeasonSyncService;
    private readonly Mock<ILeagueRoundValidator> _mockRoundValidator;
    private readonly Mock<ISyncJobRepository> _mockSyncJobRepo;
    private readonly Mock<ILogger<ProviderSyncService>> _mockLogger;
    private readonly ProviderSyncService _service;

    private static readonly Guid BetExplorerProviderId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private readonly DataProvider _betExplorerProvider;
    private readonly Country _england;
    private readonly League _premierLeague;

    public ProviderSyncServiceTests()
    {
        _mockProviderRepo = new Mock<IDataProviderRepository>();
        _mockSportRepo = new Mock<ISportRepository>();
        _mockCountryRepo = new Mock<ICountryRepository>();
        _mockLeagueRepo = new Mock<ILeagueRepository>();
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockLeagueSeasonRepo = new Mock<ILeagueSeasonRepository>();
        _mockCountryProviderRepo = new Mock<ICountryProviderRepository>();
        _mockLeagueProviderRepo = new Mock<ILeagueProviderRepository>();
        _mockCountryScraper = new Mock<ICountryScraper>();
        _mockLeagueScraper = new Mock<ILeagueMetadataScraper>();
        _mockSeasonScraper = new Mock<ISeasonScraper>();
        _mockSeasonSyncService = new Mock<ISeasonSyncService>();
        _mockRoundValidator = new Mock<ILeagueRoundValidator>();
        _mockSyncJobRepo = new Mock<ISyncJobRepository>();
        _mockLogger = new Mock<ILogger<ProviderSyncService>>();

        _betExplorerProvider = new DataProvider
        {
            Id = BetExplorerProviderId,
            Name = "BetExplorer",
            Code = "betexplorer",
            Type = ProviderType.Scraper,
            BaseUrl = "https://www.betexplorer.com",
            IsActive = true
        };

        _england = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "england",
            IsActive = true
        };

        _premierLeague = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            DisplayName = "Premier League",
            BetExplorerSlug = "premier-league",
            CountryId = _england.Id,
            Country = _england,
            IsActive = true
        };

        _service = new ProviderSyncService(
            _mockProviderRepo.Object,
            _mockSportRepo.Object,
            _mockCountryRepo.Object,
            _mockLeagueRepo.Object,
            _mockSeasonRepo.Object,
            _mockLeagueSeasonRepo.Object,
            _mockCountryProviderRepo.Object,
            _mockLeagueProviderRepo.Object,
            new[] { _mockCountryScraper.Object },
            new[] { _mockLeagueScraper.Object },
            new[] { _mockSeasonScraper.Object },
            _mockSeasonSyncService.Object,
            _mockRoundValidator.Object,
            _mockSyncJobRepo.Object,
            _mockLogger.Object
        );

        // Reset static sync status to avoid interference between tests
        _service.ResetSyncStatus();
    }

    #region GlobalSeasonScanAsync Tests

    [Fact]
    public async Task GlobalSeasonScanAsync_ScansOnlyLeaguesWithBettingProviderMapping()
    {
        // Arrange
        var leagueId = _premierLeague.Id;

        _mockProviderRepo.Setup(r => r.GetByIdAsync(BetExplorerProviderId))
            .ReturnsAsync(_betExplorerProvider);

        _mockSeasonScraper.Setup(s => s.CanHandle(_betExplorerProvider))
            .Returns(true);

        _mockLeagueProviderRepo.Setup(r => r.GetLeagueIdsWithBettingProviderMappingAsync())
            .ReturnsAsync(new List<Guid> { leagueId });

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(_premierLeague);

        _mockSeasonScraper.Setup(s => s.ScrapeAvailableSeasonsAsync(_premierLeague))
            .ReturnsAsync(new List<string> { "2023-2024", "2022-2023" });

        _mockSeasonRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => null);

        _mockSeasonRepo.Setup(r => r.AddAsync(It.IsAny<Season>()))
            .ReturnsAsync((Season s) => s);

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((LeagueSeason?)null);

        _mockLeagueSeasonRepo.Setup(r => r.AddAsync(It.IsAny<LeagueSeason>()))
            .ReturnsAsync((LeagueSeason ls) => ls);

        // Act
        var result = await _service.GlobalSeasonScanAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Success);
        Assert.Equal(2, result.Value.Statistics.TotalProcessed); // 2 seasons scraped

        // Verify it called GetLeagueIdsWithBettingProviderMappingAsync
        _mockLeagueProviderRepo.Verify(r => r.GetLeagueIdsWithBettingProviderMappingAsync(), Times.Once);
    }

    [Fact]
    public async Task GlobalSeasonScanAsync_WithSpecificLeagueIds_ScansOnlyThoseLeagues()
    {
        // Arrange
        var specificLeagueId = _premierLeague.Id;
        var specificLeagueIds = new List<Guid> { specificLeagueId };

        _mockProviderRepo.Setup(r => r.GetByIdAsync(BetExplorerProviderId))
            .ReturnsAsync(_betExplorerProvider);

        _mockSeasonScraper.Setup(s => s.CanHandle(_betExplorerProvider))
            .Returns(true);

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(specificLeagueId))
            .ReturnsAsync(_premierLeague);

        _mockSeasonScraper.Setup(s => s.ScrapeAvailableSeasonsAsync(_premierLeague))
            .ReturnsAsync(new List<string> { "2023-2024" });

        _mockSeasonRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => null);

        _mockSeasonRepo.Setup(r => r.AddAsync(It.IsAny<Season>()))
            .ReturnsAsync((Season s) => s);

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((LeagueSeason?)null);

        _mockLeagueSeasonRepo.Setup(r => r.AddAsync(It.IsAny<LeagueSeason>()))
            .ReturnsAsync((LeagueSeason ls) => ls);

        // Act
        var result = await _service.GlobalSeasonScanAsync(specificLeagueIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        // Verify it did NOT call GetLeagueIdsWithBettingProviderMappingAsync when specific IDs provided
        _mockLeagueProviderRepo.Verify(r => r.GetLeagueIdsWithBettingProviderMappingAsync(), Times.Never);

        // Verify it scanned the specific league
        _mockSeasonScraper.Verify(s => s.ScrapeAvailableSeasonsAsync(_premierLeague), Times.Once);
    }

    [Fact]
    public async Task GlobalSeasonScanAsync_ReturnsAggregatedStatistics()
    {
        // Arrange
        var leagueId = _premierLeague.Id;
        var existingSeason = new Season { Id = Guid.NewGuid(), Name = "2022-2023", StartYear = 2022, EndYear = 2023 };
        var existingLeagueSeason = new LeagueSeason
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            SeasonId = existingSeason.Id,
            IsAvailableOnBetExplorer = true
        };

        _mockProviderRepo.Setup(r => r.GetByIdAsync(BetExplorerProviderId))
            .ReturnsAsync(_betExplorerProvider);

        _mockSeasonScraper.Setup(s => s.CanHandle(_betExplorerProvider))
            .Returns(true);

        _mockLeagueProviderRepo.Setup(r => r.GetLeagueIdsWithBettingProviderMappingAsync())
            .ReturnsAsync(new List<Guid> { leagueId });

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(_premierLeague);

        // Return 3 seasons: 1 new, 1 existing with update needed, 1 existing no update
        _mockSeasonScraper.Setup(s => s.ScrapeAvailableSeasonsAsync(_premierLeague))
            .ReturnsAsync(new List<string> { "2023-2024", "2022-2023", "2021-2022" });

        // 2023-2024: New season
        _mockSeasonRepo.Setup(r => r.GetByNameAsync("2023-2024"))
            .ReturnsAsync((Season?)null);

        // 2022-2023: Existing season
        _mockSeasonRepo.Setup(r => r.GetByNameAsync("2022-2023"))
            .ReturnsAsync(existingSeason);

        // 2021-2022: New season
        _mockSeasonRepo.Setup(r => r.GetByNameAsync("2021-2022"))
            .ReturnsAsync((Season?)null);

        _mockSeasonRepo.Setup(r => r.AddAsync(It.IsAny<Season>()))
            .ReturnsAsync((Season s) => s);

        // LeagueSeason: 2022-2023 exists with IsAvailableOnBetExplorer = true (skip)
        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(leagueId, existingSeason.Id))
            .ReturnsAsync(existingLeagueSeason);

        // LeagueSeason: Others don't exist
        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(leagueId, It.Is<Guid>(id => id != existingSeason.Id)))
            .ReturnsAsync((LeagueSeason?)null);

        _mockLeagueSeasonRepo.Setup(r => r.AddAsync(It.IsAny<LeagueSeason>()))
            .ReturnsAsync((LeagueSeason ls) => ls);

        // Act
        var result = await _service.GlobalSeasonScanAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var stats = result.Value.Statistics;

        Assert.Equal(3, stats.TotalProcessed); // 3 seasons scraped
        Assert.Equal(4, stats.Created); // 2 new seasons + 2 new LeagueSeasons
        Assert.Equal(0, stats.Updated); // No updates needed
        Assert.Equal(1, stats.Skipped); // 1 LeagueSeason already exists with IsAvailableOnBetExplorer = true
        Assert.Equal(0, stats.Errors);
    }

    [Fact]
    public async Task GlobalSeasonScanAsync_NoLeaguesWithBettingProviderMapping_ReturnsEmptySuccess()
    {
        // Arrange
        _mockProviderRepo.Setup(r => r.GetByIdAsync(BetExplorerProviderId))
            .ReturnsAsync(_betExplorerProvider);

        _mockSeasonScraper.Setup(s => s.CanHandle(_betExplorerProvider))
            .Returns(true);

        _mockLeagueProviderRepo.Setup(r => r.GetLeagueIdsWithBettingProviderMappingAsync())
            .ReturnsAsync(new List<Guid>());

        // Act
        var result = await _service.GlobalSeasonScanAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Success);
        Assert.Contains("No leagues found", result.Value.Message);
        Assert.Equal(0, result.Value.Statistics.TotalProcessed);
    }

    [Fact]
    public async Task GlobalSeasonScanAsync_BetExplorerProviderNotFound_ReturnsFailure()
    {
        // Arrange
        _mockProviderRepo.Setup(r => r.GetByIdAsync(BetExplorerProviderId))
            .ReturnsAsync((DataProvider?)null);

        // Act
        var result = await _service.GlobalSeasonScanAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("BetExplorer provider not found", result.Error);
    }

    #endregion
}
