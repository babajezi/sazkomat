using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Tests.DataImport;

public class RoundRepositoryTests : IDisposable
{
    private readonly DataImportDbContext _context;
    private readonly RoundRepository _repository;
    private readonly Guid _testLeagueId;

    public RoundRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataImportDbContext(options);
        _repository = new RoundRepository(_context);
        _testLeagueId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRounds()
    {
        // Arrange
        var round1 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3,
            CumulativeOddsHome = 1.5m,
            CumulativeOddsDraw = 3.0m,
            CumulativeOddsAway = 2.5m,
            SummaryResult = "4-3-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        var round2 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 2,
            MatchesCount = 10,
            HomeWins = 5,
            Draws = 2,
            AwayWins = 3,
            CumulativeOddsHome = 1.6m,
            CumulativeOddsDraw = 3.2m,
            CumulativeOddsAway = 2.4m,
            SummaryResult = "5-2-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        await _context.Rounds.AddRangeAsync(round1, round2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRound_ReturnsRound()
    {
        // Arrange
        var round = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3,
            CumulativeOddsHome = 1.5m,
            CumulativeOddsDraw = 3.0m,
            CumulativeOddsAway = 2.5m,
            SummaryResult = "4-3-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        await _context.Rounds.AddAsync(round);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(round.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(round.Id, result.Id);
        Assert.Equal("2023/2024", result.Season);
        Assert.Equal(1, result.RoundNumber);
    }

    [Fact]
    public async Task GetByLeagueAsync_FiltersByLeague()
    {
        // Arrange
        var leagueId1 = Guid.NewGuid();
        var leagueId2 = Guid.NewGuid();

        var round1 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId1,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3,
            CumulativeOddsHome = 1.5m,
            CumulativeOddsDraw = 3.0m,
            CumulativeOddsAway = 2.5m,
            SummaryResult = "4-3-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        var round2 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId2,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 5,
            Draws = 2,
            AwayWins = 3,
            CumulativeOddsHome = 1.6m,
            CumulativeOddsDraw = 3.2m,
            CumulativeOddsAway = 2.4m,
            SummaryResult = "5-2-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        await _context.Rounds.AddRangeAsync(round1, round2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByLeagueAsync(leagueId1);

        // Assert
        Assert.Single(result);
        Assert.Equal(leagueId1, result[0].LeagueId);
    }

    [Fact]
    public async Task GetByLeagueSeasonRoundAsync_FindsSpecificRound()
    {
        // Arrange
        var round1 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3,
            CumulativeOddsHome = 1.5m,
            CumulativeOddsDraw = 3.0m,
            CumulativeOddsAway = 2.5m,
            SummaryResult = "4-3-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        var round2 = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 2,
            MatchesCount = 10,
            HomeWins = 5,
            Draws = 2,
            AwayWins = 3,
            CumulativeOddsHome = 1.6m,
            CumulativeOddsDraw = 3.2m,
            CumulativeOddsAway = 2.4m,
            SummaryResult = "5-2-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        await _context.Rounds.AddRangeAsync(round1, round2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByLeagueSeasonRoundAsync(_testLeagueId, "2023/2024", 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2023/2024", result.Season);
        Assert.Equal(1, result.RoundNumber);
    }

    [Fact]
    public async Task CreateAsync_ValidRound_AddsRound()
    {
        // Arrange
        var round = new Round
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Season = "2023/2024",
            RoundNumber = 1,
            MatchesCount = 10,
            HomeWins = 4,
            Draws = 3,
            AwayWins = 3,
            CumulativeOddsHome = 1.5m,
            CumulativeOddsDraw = 3.0m,
            CumulativeOddsAway = 2.5m,
            SummaryResult = "4-3-3",
            OddsComplete = "Yes",
            ScrapedAt = DateTime.UtcNow,
            DataSource = "betexplorer.com"
        };

        // Act
        await _repository.CreateAsync(round);
        var result = await _context.Rounds.FindAsync(round.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2023/2024", result.Season);
        Assert.Equal(1, result.RoundNumber);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
