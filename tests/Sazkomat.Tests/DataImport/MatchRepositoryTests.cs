using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;
using Sazkomat.Tests.Helpers;

namespace Sazkomat.Tests.DataImport;

public class MatchRepositoryTests : IDisposable
{
    private readonly DataImportDbContext _context;
    private readonly MatchRepository _repository;
    private readonly Guid _roundId;
    private readonly Guid _providerId;

    public MatchRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataImportDbContext(options);
        _repository = new MatchRepository(_context);
        _roundId = Guid.NewGuid();
        _providerId = Guid.NewGuid();
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_ExistingMatch_ReturnsMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            ProviderId = _providerId,
            HomeTeam = "Manchester United",
            AwayTeam = "Liverpool",
            HomeScore = 2,
            AwayScore = 1,
            Result = "H",
            HomeOdds = 2.10m,
            DrawOdds = 3.20m,
            AwayOdds = 3.50m,
            ProviderUrl = "https://www.betexplorer.com/football/england/premier-league/match-123/"
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

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_NonExistingMatch_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task CreateAsync_ValidMatch_AddsMatch()
    {
        // Arrange
        var match = new Match
        {
            RoundId = _roundId,
            ProviderId = _providerId,
            HomeTeam = "Arsenal",
            AwayTeam = "Chelsea",
            HomeScore = 1,
            AwayScore = 1,
            Result = "D",
            HomeOdds = 1.90m,
            DrawOdds = 3.40m,
            AwayOdds = 4.20m
        };

        // Act
        var result = await _repository.CreateAsync(match);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Matches.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Arsenal", saved.HomeTeam);
        Assert.Equal(1, saved.HomeScore);
        Assert.Equal(1, saved.AwayScore);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task UpdateAsync_ExistingMatch_UpdatesMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            ProviderId = _providerId,
            HomeTeam = "Manchester City",
            AwayTeam = "Tottenham",
            HomeScore = 0,
            AwayScore = 0,
            Result = "D"
        };

        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();

        // Act
        match.HomeScore = 2;
        match.AwayScore = 1;
        match.Result = "H";
        match.HomeOdds = 1.85m;
        match.DrawOdds = 3.50m;
        match.AwayOdds = 4.00m;
        await _repository.UpdateAsync(match);

        // Assert
        var updated = await _context.Matches.FindAsync(match.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.HomeScore);
        Assert.Equal(1, updated.AwayScore);
        Assert.Equal("H", updated.Result);
        Assert.Equal(1.85m, updated.HomeOdds);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task DeleteAsync_ExistingMatch_DeletesMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = Guid.NewGuid(),
            RoundId = _roundId,
            ProviderId = _providerId,
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            HomeScore = 1,
            AwayScore = 0,
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

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
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
                ProviderId = _providerId,
                HomeTeam = "Team A",
                AwayTeam = "Team B",
                HomeScore = 2,
                AwayScore = 1,
                Result = "H"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = round1,
                ProviderId = _providerId,
                HomeTeam = "Team C",
                AwayTeam = "Team D",
                HomeScore = 1,
                AwayScore = 1,
                Result = "D"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = round2,
                ProviderId = _providerId,
                HomeTeam = "Team E",
                AwayTeam = "Team F",
                HomeScore = 0,
                AwayScore = 2,
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

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByRoundIdAsync_NoMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByRoundIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
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
                ProviderId = _providerId,
                HomeTeam = "Team 1",
                AwayTeam = "Team 2",
                HomeScore = 3,
                AwayScore = 0,
                Result = "H"
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                ProviderId = _providerId,
                HomeTeam = "Team 3",
                AwayTeam = "Team 4",
                HomeScore = 1,
                AwayScore = 2,
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

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task CreateAsync_WithOdds_StoresOddsCorrectly()
    {
        // Arrange
        var match = new Match
        {
            RoundId = _roundId,
            ProviderId = _providerId,
            HomeTeam = "Barcelona",
            AwayTeam = "Real Madrid",
            HomeScore = 2,
            AwayScore = 3,
            Result = "A",
            HomeOdds = 2.05m,
            DrawOdds = 3.65m,
            AwayOdds = 3.25m,
            MatchDate = new DateTime(2024, 10, 26)
        };

        // Act
        await _repository.CreateAsync(match);

        // Assert
        var saved = await _context.Matches.FindAsync(match.Id);
        Assert.NotNull(saved);
        Assert.Equal(2.05m, saved.HomeOdds);
        Assert.Equal(3.65m, saved.DrawOdds);
        Assert.Equal(3.25m, saved.AwayOdds);
        Assert.Equal(new DateTime(2024, 10, 26), saved.MatchDate);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
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
                ProviderId = _providerId,
                HomeTeam = "Team C",
                AwayTeam = "Team D",
                HomeScore = 1,
                AwayScore = 1,
                Result = "D",
                MatchDate = new DateTime(2024, 10, 28)
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                ProviderId = _providerId,
                HomeTeam = "Team A",
                AwayTeam = "Team B",
                HomeScore = 2,
                AwayScore = 1,
                Result = "H",
                MatchDate = new DateTime(2024, 10, 26)
            },
            new()
            {
                Id = Guid.NewGuid(),
                RoundId = _roundId,
                ProviderId = _providerId,
                HomeTeam = "Team E",
                AwayTeam = "Team F",
                HomeScore = 0,
                AwayScore = 0,
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
