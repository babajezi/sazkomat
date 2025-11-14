using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Tests.DataImport;

public class LiveSyncServiceTests
{
    private readonly Mock<ISyncJobRepository> _mockSyncJobRepo;
    private readonly Mock<IDataProviderRepository> _mockDataProviderRepo;
    private readonly Mock<ILeagueRepository> _mockLeagueRepo;
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<ILeagueSeasonRepository> _mockLeagueSeasonRepo;
    private readonly Mock<IRoundRepository> _mockRoundRepo;
    private readonly Mock<IMatchRepository> _mockMatchRepo;
    private readonly Mock<ILeagueScraper> _mockScraper;
    private readonly Mock<ILogger<LiveSyncService>> _mockLogger;
    private readonly LiveSyncService _service;

    private readonly Guid _providerId;
    private readonly DataProvider _provider;
    private readonly League _league;
    private readonly Season _season;
    private readonly LeagueSeason _leagueSeason;

    public LiveSyncServiceTests()
    {
        _mockSyncJobRepo = new Mock<ISyncJobRepository>();
        _mockDataProviderRepo = new Mock<IDataProviderRepository>();
        _mockLeagueRepo = new Mock<ILeagueRepository>();
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockLeagueSeasonRepo = new Mock<ILeagueSeasonRepository>();
        _mockRoundRepo = new Mock<IRoundRepository>();
        _mockMatchRepo = new Mock<IMatchRepository>();
        _mockScraper = new Mock<ILeagueScraper>();
        _mockLogger = new Mock<ILogger<LiveSyncService>>();

        _providerId = Guid.NewGuid();
        _provider = new DataProvider
        {
            Id = _providerId,
            Name = "BetExplorer",
            Code = "betexplorer",
            Type = ProviderType.Scraper,
            IsActive = true
        };

        _league = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            BetExplorerSlug = "england/premier-league",
            SportId = Guid.NewGuid(),
            CountryId = Guid.NewGuid(),
            IsSyncEnabled = true
        };

        _season = new Season
        {
            Id = Guid.NewGuid(),
            Name = "2023-2024",
            DisplayName = "2023/2024",
            StartYear = 2023,
            EndYear = 2024
        };

        _leagueSeason = new LeagueSeason
        {
            Id = Guid.NewGuid(),
            LeagueId = _league.Id,
            SeasonId = _season.Id,
            IsCurrent = true
        };

        _service = new LiveSyncService(
            _mockSyncJobRepo.Object,
            _mockDataProviderRepo.Object,
            _mockLeagueRepo.Object,
            _mockSeasonRepo.Object,
            _mockLeagueSeasonRepo.Object,
            _mockRoundRepo.Object,
            _mockMatchRepo.Object,
            _mockScraper.Object,
            _mockLogger.Object
        );
    }

    #region LiveSyncRounds Tests

    [Fact]
    public async Task LiveSyncRoundsAsync_ProviderNotFound_ThrowsException()
    {
        // Arrange
        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync((DataProvider?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.LiveSyncRoundsAsync(_providerId)
        );
    }

    [Fact]
    public async Task LiveSyncRoundsAsync_ValidProvider_CreatesSyncJob()
    {
        // Arrange
        var createdJob = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending,
            Priority = 10
        };

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockSyncJobRepo.Setup(r => r.CreateAsync(It.IsAny<SyncJob>()))
            .ReturnsAsync(createdJob);

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(createdJob.Id))
            .ReturnsAsync(createdJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<League>());

        // Act
        var jobId = await _service.LiveSyncRoundsAsync(_providerId);

        // Assert
        Assert.Equal(createdJob.Id, jobId);
        _mockSyncJobRepo.Verify(r => r.CreateAsync(It.Is<SyncJob>(j =>
            j.ProviderId == _providerId &&
            j.Type == SyncJobType.LiveUpdate &&
            j.EntityType == SyncEntityType.Rounds &&
            j.Priority == 10
        )), Times.Once);
    }

    [Fact]
    public async Task LiveSyncRoundsInternalAsync_NoActiveSeasons_SkipsLeagues()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<League> { _league });

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueIdAsync(_league.Id, true))
            .ReturnsAsync(new List<LeagueSeason>()); // No active seasons

        // Act
        await _service.LiveSyncRoundsInternalAsync(jobId, _providerId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockScraper.Verify(s => s.ScrapeSeasonAsync(It.IsAny<League>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LiveSyncRoundsInternalAsync_CreatesNewRounds()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        var scrapedRounds = new List<Round>
        {
            new()
            {
                RoundNumber = 1,
                MatchesCount = 10,
                HomeWins = 4,
                Draws = 3,
                AwayWins = 3,
                CumulativeOddsHome = 18.5m,
                CumulativeOddsDraw = 32.0m,
                CumulativeOddsAway = 25.5m,
                SummaryResult = "4-3-3",
                OddsComplete = "Yes",
                Matches = new List<Match>
                {
                    new() { HomeTeam = "Team A", AwayTeam = "Team B", Result = "H", Odds1 = 1.85m, OddsX = 3.2m, Odds2 = 2.55m }
                }
            }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<League> { _league });

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueIdAsync(_league.Id, true))
            .ReturnsAsync(new List<LeagueSeason> { _leagueSeason });

        _mockSeasonRepo.Setup(r => r.GetByIdAsync(_season.Id))
            .ReturnsAsync(_season);

        _mockScraper.Setup(s => s.ScrapeSeasonAsync(_league, _season.Name))
            .ReturnsAsync(scrapedRounds);

        _mockRoundRepo.Setup(r => r.GetByLeagueAndSeasonAsync(_league.Id, _season.Id))
            .ReturnsAsync(new List<Round>()); // No existing rounds

        var capturedRound = new Round();
        _mockRoundRepo.Setup(r => r.CreateAsync(It.IsAny<Round>()))
            .ReturnsAsync((Round r) => { capturedRound = r; return r; });

        // Act
        await _service.LiveSyncRoundsInternalAsync(jobId, _providerId);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockRoundRepo.Verify(r => r.CreateAsync(It.IsAny<Round>()), Times.Once);
        _mockMatchRepo.Verify(r => r.CreateAsync(It.IsAny<Match>()), Times.Once);
    }

    [Fact]
    public async Task LiveSyncRoundsInternalAsync_WithForceRefresh_UpdatesExistingRounds()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        var existingRound = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _league.Id,
            SeasonId = _season.Id,
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3
        };

        var scrapedRounds = new List<Round>
        {
            new()
            {
                RoundNumber = 1,
                MatchesCount = 10,
                HomeWins = 5, // Updated
                Draws = 2,    // Updated
                AwayWins = 3,
                CumulativeOddsHome = 19.0m,
                CumulativeOddsDraw = 30.0m,
                CumulativeOddsAway = 25.0m,
                SummaryResult = "5-2-3",
                OddsComplete = "Yes",
                Matches = new List<Match>()
            }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<League> { _league });

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueIdAsync(_league.Id, true))
            .ReturnsAsync(new List<LeagueSeason> { _leagueSeason });

        _mockSeasonRepo.Setup(r => r.GetByIdAsync(_season.Id))
            .ReturnsAsync(_season);

        _mockScraper.Setup(s => s.ScrapeSeasonAsync(_league, _season.Name))
            .ReturnsAsync(scrapedRounds);

        _mockRoundRepo.Setup(r => r.GetByLeagueAndSeasonAsync(_league.Id, _season.Id))
            .ReturnsAsync(new List<Round> { existingRound });

        _mockMatchRepo.Setup(r => r.GetByRoundIdAsync(existingRound.Id))
            .ReturnsAsync(new List<Match>());

        // Act
        await _service.LiveSyncRoundsInternalAsync(jobId, _providerId, null, forceRefresh: true);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.Equal(5, existingRound.HomeWins); // Verify update
        Assert.Equal(2, existingRound.Draws);
        _mockRoundRepo.Verify(r => r.UpdateAsync(existingRound), Times.Once);
        _mockRoundRepo.Verify(r => r.CreateAsync(It.IsAny<Round>()), Times.Never);
    }

    [Fact]
    public async Task LiveSyncRoundsInternalAsync_WithoutForceRefresh_SkipsExistingRounds()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        var existingRound = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _league.Id,
            SeasonId = _season.Id,
            RoundNumber = 1,
            MatchesCount = 10
        };

        var scrapedRounds = new List<Round>
        {
            new()
            {
                RoundNumber = 1,
                MatchesCount = 10,
                Matches = new List<Match>()
            }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<League> { _league });

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueIdAsync(_league.Id, true))
            .ReturnsAsync(new List<LeagueSeason> { _leagueSeason });

        _mockSeasonRepo.Setup(r => r.GetByIdAsync(_season.Id))
            .ReturnsAsync(_season);

        _mockScraper.Setup(s => s.ScrapeSeasonAsync(_league, _season.Name))
            .ReturnsAsync(scrapedRounds);

        _mockRoundRepo.Setup(r => r.GetByLeagueAndSeasonAsync(_league.Id, _season.Id))
            .ReturnsAsync(new List<Round> { existingRound });

        // Act
        await _service.LiveSyncRoundsInternalAsync(jobId, _providerId, null, forceRefresh: false);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        _mockRoundRepo.Verify(r => r.UpdateAsync(It.IsAny<Round>()), Times.Never);
        _mockRoundRepo.Verify(r => r.CreateAsync(It.IsAny<Round>()), Times.Never);
    }

    [Fact]
    public async Task LiveSyncRoundsInternalAsync_OnError_MarksJobAsFailed()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockLeagueRepo.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.LiveSyncRoundsInternalAsync(jobId, _providerId)
        );

        Assert.Equal(SyncJobStatus.Failed, syncJob.Status);
        Assert.NotNull(syncJob.ErrorMessage);
        Assert.Contains("Database error", syncJob.ErrorMessage);
    }

    #endregion

    #region LiveSyncRound Tests (Single Round)

    [Fact]
    public async Task LiveSyncRoundAsync_RoundNotFound_ThrowsException()
    {
        // Arrange
        var roundId = Guid.NewGuid();

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockRoundRepo.Setup(r => r.GetByIdAsync(roundId))
            .ReturnsAsync((Round?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.LiveSyncRoundAsync(_providerId, roundId)
        );
    }

    [Fact]
    public async Task LiveSyncRoundInternalAsync_UpdatesSingleRound()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var syncJob = new SyncJob
        {
            Id = jobId,
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            EntityType = SyncEntityType.Rounds,
            Status = SyncJobStatus.Pending
        };

        var existingRound = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _league.Id,
            SeasonId = _season.Id,
            RoundNumber = 5,
            MatchesCount = 10,
            HomeWins = 4
        };

        var scrapedRounds = new List<Round>
        {
            new()
            {
                RoundNumber = 5,
                MatchesCount = 10,
                HomeWins = 6, // Updated
                Draws = 2,
                AwayWins = 2,
                CumulativeOddsHome = 20.0m,
                CumulativeOddsDraw = 28.0m,
                CumulativeOddsAway = 22.0m,
                SummaryResult = "6-2-2",
                OddsComplete = "Yes",
                Matches = new List<Match>
                {
                    new() { HomeTeam = "Team A", AwayTeam = "Team B", Result = "H", Odds1 = 2.0m, OddsX = 2.8m, Odds2 = 2.2m }
                }
            }
        };

        _mockSyncJobRepo.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(syncJob);

        _mockRoundRepo.Setup(r => r.GetByIdAsync(existingRound.Id))
            .ReturnsAsync(existingRound);

        _mockLeagueRepo.Setup(r => r.GetByIdAsync(_league.Id))
            .ReturnsAsync(_league);

        _mockSeasonRepo.Setup(r => r.GetByIdAsync(_season.Id))
            .ReturnsAsync(_season);

        _mockScraper.Setup(s => s.ScrapeSeasonAsync(_league, _season.Name))
            .ReturnsAsync(scrapedRounds);

        _mockMatchRepo.Setup(r => r.GetByRoundIdAsync(existingRound.Id))
            .ReturnsAsync(new List<Match>());

        // Act
        await _service.LiveSyncRoundInternalAsync(jobId, _providerId, existingRound.Id);

        // Assert
        Assert.Equal(SyncJobStatus.Completed, syncJob.Status);
        Assert.Equal(6, existingRound.HomeWins); // Verify update
        _mockRoundRepo.Verify(r => r.UpdateAsync(existingRound), Times.Once);
        _mockMatchRepo.Verify(r => r.CreateAsync(It.IsAny<Match>()), Times.Once);
    }

    #endregion

    #region GetLiveSyncStats Tests

    [Fact]
    public async Task GetLiveSyncStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        var leagueSeasons = new List<LeagueSeason>
        {
            new() { Id = Guid.NewGuid(), LeagueId = _league.Id, SeasonId = _season.Id, IsCurrent = true },
            new() { Id = Guid.NewGuid(), LeagueId = Guid.NewGuid(), SeasonId = Guid.NewGuid(), IsCurrent = true }
        };

        var rounds = new List<Round>
        {
            new() { Id = Guid.NewGuid(), RoundNumber = 1, ScrapedAt = DateTime.UtcNow.AddHours(-25) }, // Needs update
            new() { Id = Guid.NewGuid(), RoundNumber = 2, ScrapedAt = DateTime.UtcNow.AddHours(-1) }   // Recent
        };

        var recentJob = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.LiveUpdate,
            Status = SyncJobStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-2)
        };

        _mockDataProviderRepo.Setup(r => r.GetByIdAsync(_providerId))
            .ReturnsAsync(_provider);

        _mockLeagueSeasonRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(leagueSeasons);

        _mockRoundRepo.Setup(r => r.GetByLeagueAndSeasonAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(rounds);

        _mockSyncJobRepo.Setup(r => r.GetRecentJobsAsync(_providerId, 10))
            .ReturnsAsync(new List<SyncJob> { recentJob });

        // Act
        var stats = await _service.GetLiveSyncStatsAsync(_providerId);

        // Assert
        Assert.Equal(2, stats.ActiveLeagues);
        Assert.Equal(4, stats.TotalRounds); // 2 leagues * 2 rounds each
        Assert.True(stats.RoundsNeedingUpdate >= 2); // At least 2 leagues * 1 old round
        Assert.Equal(recentJob.CompletedAt, stats.LastSyncAt);
    }

    #endregion
}
