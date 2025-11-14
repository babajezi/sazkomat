using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Tests.DataImport;

public class MatchRepositoryTests : IDisposable
{
    private readonly DataImportDbContext _context;
    private readonly MatchRepository _repository;
    private readonly Guid _roundId;

    public MatchRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataImportDbContext(options);
        _repository = new MatchRepository(_context);
        _roundId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMatch_ReturnsMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            HomeTeam = "Manchester United",
            AwayTeam = "Liverpool",
            Score = "2:1",
            Result = "H",
            Odds1 = 2.10m,
            OddsX = 3.20m,
            Odds2 = 3.50m,
            BetExplorerUrl = "https://www.betexplorer.com/football/england/premier-league/match-123/"
        };

        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(match.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(match.Id, result.Id);
        Assert.Equal("Manchester United", result.HomeTeam);
        Assert.Equal("Liverpool", result.AwayTeam);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingMatch_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidMatch_AddsMatch()
    {
        // Arrange
        var match = new Match
        {
            RoundId = _roundId,
            HomeTeam = "Arsenal",
            AwayTeam = "Chelsea",
            Score = "1:1",
            Result = "D",
            Odds1 = 1.90m,
            OddsX = 3.40m,
            Odds2 = 4.20m
        };

        // Act
        var result = await _repository.CreateAsync(match);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Matches.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Arsenal", saved.HomeTeam);
        Assert.Equal("1:1", saved.Score);
    }

    [Fact]
    public async Task UpdateAsync_ExistingMatch_UpdatesMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            HomeTeam = "Manchester City",
            AwayTeam = "Tottenham",
            Score = "0:0",
            Result = "D"
        };

        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        // Act
        match.Score = "2:1";
        match.Result = "H";
        match.Odds1 = 1.85m;
        match.OddsX = 3.50m;
        match.Odds2 = 4.00m;
        await _repository.UpdateAsync(match);

        // Assert
        var updated = await _context.Matches.FindAsync(match.Id);
        Assert.NotNull(updated);
        Assert.Equal("2:1", updated.Score);
        Assert.Equal("H", updated.Result);
        Assert.Equal(1.85m, updated.Odds1);
    }

    [Fact]
    public async Task DeleteAsync_ExistingMatch_DeletesMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            Score = "1:0",
            Result = "H"
        };

        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(match.Id);
        var result = await _context.Matches.FindAsync(match.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByRoundIdAsync_ReturnsMatchesForRound()
    {
        // Arrange
        var round1 = Guid.NewGuid();
        var round2 = Guid.NewGuid();

        var matches = new List<Match>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = round1,
                HomeTeam = "Team A",
                AwayTeam = "Team B",
                Score = "2:1",
                Result = "H"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = round1,
                HomeTeam = "Team C",
                AwayTeam = "Team D",
                Score = "1:1",
                Result = "D"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = round2,
                HomeTeam = "Team E",
                AwayTeam = "Team F",
                Score = "0:2",
                Result = "A"
            }
        };

        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRoundIdAsync(round1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(round1, m.RoundId));
    }

    [Fact]
    public async Task GetByRoundIdAsync_NoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByRoundIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMatches()
    {
        // Arrange
        var matches = new List<Match>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                HomeTeam = "Team 1",
                AwayTeam = "Team 2",
                Score = "3:0",
                Result = "H"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                HomeTeam = "Team 3",
                AwayTeam = "Team 4",
                Score = "1:2",
                Result = "A"
            }
        };

        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAsync_WithOdds_StoresOddsCorrectly()
    {
        // Arrange
        var match = new Match
        {
            RoundId = _roundId,
            HomeTeam = "Barcelona",
            AwayTeam = "Real Madrid",
            Score = "2:3",
            Result = "A",
            Odds1 = 2.05m,
            OddsX = 3.65m,
            Odds2 = 3.25m,
            MatchDate = new DateTime(2024, 10, 26)
        };

        // Act
        await _repository.CreateAsync(match);

        // Assert
        var saved = await _context.Matches.FindAsync(match.Id);
        Assert.NotNull(saved);
        Assert.Equal(2.05m, saved.Odds1);
        Assert.Equal(3.65m, saved.OddsX);
        Assert.Equal(3.25m, saved.Odds2);
        Assert.Equal(new DateTime(2024, 10, 26), saved.MatchDate);
    }

    [Fact]
    public async Task GetByRoundIdAsync_OrdersMatches()
    {
        // Arrange
        var matches = new List<Match>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                HomeTeam = "Team C",
                AwayTeam = "Team D",
                Score = "1:1",
                Result = "D",
                MatchDate = new DateTime(2024, 10, 28)
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                HomeTeam = "Team A",
                AwayTeam = "Team B",
                Score = "2:1",
                Result = "H",
                MatchDate = new DateTime(2024, 10, 26)
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                HomeTeam = "Team E",
                AwayTeam = "Team F",
                Score = "0:0",
                Result = "D",
                MatchDate = new DateTime(2024, 10, 27)
            }
        };

        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRoundIdAsync(_roundId);

        // Assert
        Assert.Equal(3, result.Count);
        // Verify chronological order
        Assert.True(result[0].MatchDate <= result[1].MatchDate);
        Assert.True(result[1].MatchDate <= result[2].MatchDate);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
