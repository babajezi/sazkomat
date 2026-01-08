using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Tests.Helpers;

namespace Sazkomat.Tests.Configuration;

public class LeagueRepositoryTests : IDisposable
{
    private readonly ConfigurationDbContext _context;
    private readonly LeagueRepository _repository;

    public LeagueRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConfigurationDbContext(options);
        _repository = new LeagueRepository(_context, TestHelpers.CreateMockLogger<LeagueRepository>());

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var sport = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Football",
            Code = "FOOT",
            IsActive = true
        };

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = "England",
            Code = "ENG",
            FlagEmoji = "🏴"
        };

        _context.Sports.Add(sport);
        _context.Countries.Add(country);
        _context.SaveChanges();
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetAllAsync_ReturnsAllLeagues()
    {
        // Arrange
        var sport = await _context.Sports.FirstAsync();
        var country = await _context.Countries.FirstAsync();

        var league1 = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "Premier League",
            DisplayName = "English Premier League",
            BetExplorerSlug = "england/premier-league",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        var league2 = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "Championship",
            DisplayName = "English Championship",
            BetExplorerSlug = "england/championship",
            IsActive = false,
            IsBettable = false,
            Priority = 2
        };

        await _context.Leagues.AddRangeAsync(league1, league2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_ExistingLeague_ReturnsLeague()
    {
        // Arrange
        var sport = await _context.Sports.FirstAsync();
        var country = await _context.Countries.FirstAsync();

        var league = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "Premier League",
            DisplayName = "English Premier League",
            BetExplorerSlug = "england/premier-league",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        await _context.Leagues.AddAsync(league);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(league.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(league.Id, result.Id);
        Assert.Equal("Premier League", result.Name);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_NonExistingLeague_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task CreateAsync_ValidLeague_AddsLeague()
    {
        // Arrange
        var sport = await _context.Sports.FirstAsync();
        var country = await _context.Countries.FirstAsync();

        var league = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "La Liga",
            DisplayName = "Spanish La Liga",
            BetExplorerSlug = "spain/la-liga",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        // Act
        await _repository.CreateAsync(league);
        var result = await _context.Leagues.FindAsync(league.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("La Liga", result.Name);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task UpdateAsync_ExistingLeague_UpdatesLeague()
    {
        // Arrange
        var sport = await _context.Sports.FirstAsync();
        var country = await _context.Countries.FirstAsync();

        var league = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "Premier League",
            DisplayName = "English Premier League",
            BetExplorerSlug = "england/premier-league",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        await _context.Leagues.AddAsync(league);
        await _context.SaveChangesAsync();

        // Act
        league.DisplayName = "Updated Premier League";
        league.Priority = 5;
        await _repository.UpdateAsync(league);

        var result = await _context.Leagues.FindAsync(league.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Premier League", result.DisplayName);
        Assert.Equal(5, result.Priority);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task DeleteAsync_ExistingLeague_DeletesLeague()
    {
        // Arrange
        var sport = await _context.Sports.FirstAsync();
        var country = await _context.Countries.FirstAsync();

        var league = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport.Id,
            CountryId = country.Id,
            Name = "Premier League",
            DisplayName = "English Premier League",
            BetExplorerSlug = "england/premier-league",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        await _context.Leagues.AddAsync(league);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(league.Id);
        var result = await _context.Leagues.FindAsync(league.Id);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetAllAsync_FiltersBySport()
    {
        // Arrange
        var sport1 = await _context.Sports.FirstAsync();
        var sport2 = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Basketball",
            Code = "BASK",
            IsActive = true
        };
        await _context.Sports.AddAsync(sport2);
        await _context.SaveChangesAsync();

        var country = await _context.Countries.FirstAsync();

        var league1 = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport1.Id,
            CountryId = country.Id,
            Name = "Premier League",
            DisplayName = "English Premier League",
            BetExplorerSlug = "england/premier-league",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        var league2 = new League
        {
            Id = Guid.NewGuid(),
            SportId = sport2.Id,
            CountryId = country.Id,
            Name = "NBA",
            DisplayName = "NBA League",
            BetExplorerSlug = "usa/nba",
            IsActive = true,
            IsBettable = true,
            Priority = 1
        };

        await _context.Leagues.AddRangeAsync(league1, league2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(sportId: sport1.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Premier League", result[0].Name);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
