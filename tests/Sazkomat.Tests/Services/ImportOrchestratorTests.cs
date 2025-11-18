using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
using Sazkomat.DataImport.DTOs;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Tests.Services;

public class ImportOrchestratorTests
{
    private readonly Mock<ILeagueRepository> _mockLeagueRepository;
    private readonly Mock<ISeasonRepository> _mockSeasonRepository;
    private readonly Mock<IRoundRepository> _mockRoundRepository;
    private readonly Mock<IImportJobRepository> _mockImportJobRepository;
    private readonly Mock<ISeasonService> _mockSeasonService;
    private readonly Mock<ISeasonScraper> _mockSeasonScraper;
    private readonly Mock<IDataProviderRepository> _mockDataProviderRepository;
    private readonly Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<ImportOrchestrator>> _mockLogger;
    private readonly ImportOrchestrator _orchestrator;

    public ImportOrchestratorTests()
    {
        _mockLeagueRepository = new Mock<ILeagueRepository>();
        _mockSeasonRepository = new Mock<ISeasonRepository>();
        _mockRoundRepository = new Mock<IRoundRepository>();
        _mockImportJobRepository = new Mock<IImportJobRepository>();
        _mockSeasonService = new Mock<ISeasonService>();
        _mockSeasonScraper = new Mock<ISeasonScraper>();
        _mockDataProviderRepository = new Mock<IDataProviderRepository>();
        _mockScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<ImportOrchestrator>>();

        _orchestrator = new ImportOrchestrator(
            _mockLeagueRepository.Object,
            _mockSeasonRepository.Object,
            _mockRoundRepository.Object,
            _mockImportJobRepository.Object,
            _mockSeasonService.Object,
            _mockSeasonScraper.Object,
            _mockDataProviderRepository.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task StartHistoricalImportAsync_NoLeagues_ReturnsFailure()
    {
        // Arrange
        var request = new HistoricalImportRequest(
            new List<Guid>(),
            new List<string> { "2023/2024" },
            false);

        // Act
        var result = await _orchestrator.StartHistoricalImportAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("At least one league must be provided", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task StartHistoricalImportAsync_NoSeasons_ReturnsFailure()
    {
        // Arrange
        var request = new HistoricalImportRequest(
            new List<Guid> { Guid.NewGuid() },
            new List<string>(),
            false);

        // Act
        var result = await _orchestrator.StartHistoricalImportAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("At least one season must be provided", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task StartHistoricalImportAsync_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var request = new HistoricalImportRequest(
            new List<Guid> { leagueId },
            new List<string> { "2023/2024" },
            false);

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync((League?)null);

        // Act
        var result = await _orchestrator.StartHistoricalImportAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task StartHistoricalImportAsync_LeagueNotEnabled_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var league = new League
        {
            Id = leagueId,
            Name = "Test League",
            IsSyncEnabled = false,
            SportId = Guid.NewGuid(),
            CountryId = Guid.NewGuid()
        };

        var request = new HistoricalImportRequest(
            new List<Guid> { leagueId },
            new List<string> { "2023/2024" },
            false);

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(league);

        // Act
        var result = await _orchestrator.StartHistoricalImportAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("is not enabled for import", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task StartHistoricalImportAsync_ValidRequest_CreatesJob()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var sportId = Guid.NewGuid();

        var league = new League
        {
            Id = leagueId,
            Name = "Test League",
            IsSyncEnabled = true,
            SportId = sportId,
            CountryId = Guid.NewGuid(),
            Sport = new Sport
            {
                Id = sportId,
                Name = "Football",
                Code = "football",
                IsActive = true
            }
        };

        var season1Id = Guid.NewGuid();
        var season2Id = Guid.NewGuid();
        var seasonIds = new List<Guid> { season1Id, season2Id };

        var request = new HistoricalImportRequest(
            new List<Guid> { leagueId },
            new List<string> { "2023/2024", "2022/2023" },
            false);

        var createdJob = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            SeasonIds = seasonIds,
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData
            {
                TotalSeasons = 2,
                ProcessedSeasonIds = new List<Guid>(),
                ProcessedRounds = 0,
                Errors = new List<string>()
            }
        };

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(league);

        _mockImportJobRepository.Setup(r => r.CreateAsync(It.IsAny<ImportJob>()))
            .ReturnsAsync(createdJob);

        // Act
        var result = await _orchestrator.StartHistoricalImportAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(leagueId, result.Value.LeagueId);
        // Job can be Pending or Running (background task may have started)
        Assert.Contains(result.Value.Status, new[] { ImportJobStatus.Pending, ImportJobStatus.Running });
        Assert.Equal(2, result.Value.Progress.TotalSeasons);
        _mockImportJobRepository.Verify(r => r.CreateAsync(It.IsAny<ImportJob>()), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetJobStatusAsync_JobExists_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var job = new ImportJob
        {
            Id = jobId,
            LeagueId = Guid.NewGuid(),
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Running,
            SeasonIds = new List<Guid> { seasonId },
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData
            {
                TotalSeasons = 1,
                ProcessedSeasonIds = new List<Guid>(),
                ProcessedRounds = 0,
                Errors = new List<string>()
            }
        };

        _mockImportJobRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        var result = await _orchestrator.GetJobStatusAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jobId, result.Id);
        Assert.Equal(ImportJobStatus.Running, result.Status);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetJobStatusAsync_JobNotFound_ReturnsNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockImportJobRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((ImportJob?)null);

        // Act
        var result = await _orchestrator.GetJobStatusAsync(jobId);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetImportStatsAsync_NoRounds_ReturnsNull()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        _mockRoundRepository.Setup(r => r.GetByLeagueAsync(leagueId))
            .ReturnsAsync(new List<Round>());

        // Act
        var result = await _orchestrator.GetImportStatsAsync(leagueId);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetImportStatsAsync_WithRounds_ReturnsStats()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var season1Id = Guid.NewGuid();
        var season2Id = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        var rounds = new List<Round>
        {
            new Round
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                SeasonId = season1Id,
                ProviderId = providerId,
                RoundNumber = 1,
                MatchesCount = 10,
                HomeWins = 4,
                Draws = 3,
                AwayWins = 3,
                CumulativeOddsHome = 1000m,
                CumulativeOddsDraw = 2000m,
                CumulativeOddsAway = 1500m,
                SummaryResult = "4-3-3",
                OddsComplete = "Yes",
                ScrapedAt = DateTime.UtcNow
            },
            new Round
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                SeasonId = season1Id,
                ProviderId = providerId,
                RoundNumber = 2,
                MatchesCount = 10,
                HomeWins = 5,
                Draws = 2,
                AwayWins = 3,
                CumulativeOddsHome = 900m,
                CumulativeOddsDraw = 1800m,
                CumulativeOddsAway = 1600m,
                SummaryResult = "5-2-3",
                OddsComplete = "Yes",
                ScrapedAt = DateTime.UtcNow
            },
            new Round
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                SeasonId = season2Id,
                ProviderId = providerId,
                RoundNumber = 1,
                MatchesCount = 10,
                HomeWins = 6,
                Draws = 2,
                AwayWins = 2,
                CumulativeOddsHome = 800m,
                CumulativeOddsDraw = 1900m,
                CumulativeOddsAway = 1700m,
                SummaryResult = "6-2-2",
                OddsComplete = "Yes",
                ScrapedAt = DateTime.UtcNow
            }
        };

        _mockRoundRepository.Setup(r => r.GetByLeagueAsync(leagueId))
            .ReturnsAsync(rounds);

        // Act
        var result = await _orchestrator.GetImportStatsAsync(leagueId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRounds);
        Assert.Equal(2, result.TotalSeasons);
        Assert.Equal("2022/2023", result.OldestSeason);
        Assert.Equal("2023/2024", result.NewestSeason);
        Assert.Equal(2, result.RoundsBySeason["2023/2024"]);
        Assert.Equal(1, result.RoundsBySeason["2022/2023"]);
    }
}
