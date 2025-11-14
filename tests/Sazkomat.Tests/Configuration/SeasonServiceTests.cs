using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Tests.Configuration;

public class SeasonServiceTests
{
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<ILeagueSeasonRepository> _mockLeagueSeasonRepo;
    private readonly Mock<ILeagueRepository> _mockLeagueRepo;
    private readonly Mock<ILogger<SeasonService>> _mockLogger;
    private readonly SeasonService _service;

    public SeasonServiceTests()
    {
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockLeagueSeasonRepo = new Mock<ILeagueSeasonRepository>();
        _mockLeagueRepo = new Mock<ILeagueRepository>();
        _mockLogger = new Mock<ILogger<SeasonService>>();

        _service = new SeasonService(
            _mockSeasonRepo.Object,
            _mockLeagueSeasonRepo.Object,
            _mockLeagueRepo.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetAvailableSeasonsForLeagueAsync_ReturnsSeasons()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasons = new List<LeagueSeason>
        {
            new()
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                SeasonId = Guid.NewGuid(),
                IsAvailableOnBetExplorer = true,
                HasData = true,
                HasOdds = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                SeasonId = Guid.NewGuid(),
                IsAvailableOnBetExplorer = true,
                HasData = false,
                HasOdds = false
            }
        };

        _mockLeagueSeasonRepo.Setup(r => r.GetAvailableForLeagueAsync(leagueId))
            .ReturnsAsync(seasons);

        // Act
        var result = await _service.GetAvailableSeasonsForLeagueAsync(leagueId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetAvailableSeasonsForLeagueAsync_OnError_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        _mockLeagueSeasonRepo.Setup(r => r.GetAvailableForLeagueAsync(leagueId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _service.GetAvailableSeasonsForLeagueAsync(leagueId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database error", result.Error);
    }

    [Fact]
    public async Task UpdateLeagueSeasonStatsAsync_ValidRequest_UpdatesStats()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var roundsCount = 38;
        var matchesCount = 380;
        var hasOdds = true;

        // Act
        var result = await _service.UpdateLeagueSeasonStatsAsync(leagueId, seasonId, roundsCount, matchesCount, hasOdds);

        // Assert
        Assert.True(result.IsSuccess);
        _mockLeagueSeasonRepo.Verify(r => r.UpdateMetadataAsync(leagueId, seasonId, roundsCount, matchesCount, hasOdds), Times.Once);
    }

    [Fact]
    public async Task UpdateLeagueSeasonStatsAsync_OnError_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();

        _mockLeagueSeasonRepo.Setup(r => r.UpdateMetadataAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _service.UpdateLeagueSeasonStatsAsync(leagueId, seasonId, 38, 380, true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Update failed", result.Error);
    }

    [Fact]
    public async Task GetOrCreateLeagueSeasonAsync_ExistingLeagueSeason_ReturnsExisting()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonName = "2023-2024";

        var season = new Season
        {
            Id = Guid.NewGuid(),
            Name = seasonName,
            StartYear = 2023,
            EndYear = 2024
        };

        var existingLeagueSeason = new LeagueSeason
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            SeasonId = season.Id,
            IsAvailableOnBetExplorer = true,
            HasData = true
        };

        _mockSeasonRepo.Setup(r => r.GetOrCreateAsync(seasonName))
            .ReturnsAsync(season);

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(leagueId, season.Id))
            .ReturnsAsync(existingLeagueSeason);

        // Act
        var result = await _service.GetOrCreateLeagueSeasonAsync(leagueId, seasonName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existingLeagueSeason.Id, result.Value.Id);
        _mockLeagueSeasonRepo.Verify(r => r.CreateAsync(It.IsAny<LeagueSeason>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateLeagueSeasonAsync_NewLeagueSeason_CreatesNew()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonName = "2024-2025";

        var season = new Season
        {
            Id = Guid.NewGuid(),
            Name = seasonName,
            StartYear = 2024,
            EndYear = 2025
        };

        var createdLeagueSeason = new LeagueSeason
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            SeasonId = season.Id,
            IsAvailableOnBetExplorer = false,
            HasData = false,
            HasOdds = false
        };

        _mockSeasonRepo.Setup(r => r.GetOrCreateAsync(seasonName))
            .ReturnsAsync(season);

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(leagueId, season.Id))
            .ReturnsAsync((LeagueSeason?)null);

        _mockLeagueSeasonRepo.Setup(r => r.CreateAsync(It.IsAny<LeagueSeason>()))
            .ReturnsAsync(createdLeagueSeason);

        // Act
        var result = await _service.GetOrCreateLeagueSeasonAsync(leagueId, seasonName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(leagueId, result.Value.LeagueId);
        Assert.Equal(season.Id, result.Value.SeasonId);
        _mockLeagueSeasonRepo.Verify(r => r.CreateAsync(It.Is<LeagueSeason>(ls =>
            ls.LeagueId == leagueId &&
            ls.SeasonId == season.Id &&
            ls.IsAvailableOnBetExplorer == false &&
            ls.HasData == false &&
            ls.HasOdds == false
        )), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateLeagueSeasonAsync_OnError_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonName = "2023-2024";

        _mockSeasonRepo.Setup(r => r.GetOrCreateAsync(seasonName))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetOrCreateLeagueSeasonAsync(leagueId, seasonName);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database error", result.Error);
    }

    [Fact]
    public async Task GetOrCreateLeagueSeasonAsync_CreatesSeasonIfNotExists()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var seasonName = "2025-2026";

        var newSeason = new Season
        {
            Id = Guid.NewGuid(),
            Name = seasonName,
            StartYear = 2025,
            EndYear = 2026
        };

        _mockSeasonRepo.Setup(r => r.GetOrCreateAsync(seasonName))
            .ReturnsAsync(newSeason);

        _mockLeagueSeasonRepo.Setup(r => r.GetByLeagueAndSeasonAsync(leagueId, newSeason.Id))
            .ReturnsAsync((LeagueSeason?)null);

        _mockLeagueSeasonRepo.Setup(r => r.CreateAsync(It.IsAny<LeagueSeason>()))
            .ReturnsAsync((LeagueSeason ls) => ls);

        // Act
        var result = await _service.GetOrCreateLeagueSeasonAsync(leagueId, seasonName);

        // Assert
        Assert.True(result.IsSuccess);
        _mockSeasonRepo.Verify(r => r.GetOrCreateAsync(seasonName), Times.Once);
    }
}
