using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Tests.Helpers;

namespace Sazkomat.Tests.Data;

public class ProviderLeagueRepositoryTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly ProviderLeagueRepository _repository;
    private readonly Guid _providerId;

    public ProviderLeagueRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _repository = new ProviderLeagueRepository(_context, TestHelpers.CreateMockLogger<ProviderLeagueRepository>());
        _providerId = Guid.NewGuid();
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_ExistingLeague_ReturnsLeague()
    {
        // Arrange
        var league = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "england/premier-league",
            ProviderName = "Premier League",
            DisplayName = "English Premier League",
            IsBettable = true,
            Priority = 1,
            IsImported = false
        };

        await _context.ProviderLeagues.AddAsync(league);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(league.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(league.Id, result.Id);
        Assert.Equal("Premier League", result.ProviderName);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByProviderIdAsync_ReturnsAllLeaguesForProvider()
    {
        // Arrange
        var provider1 = Guid.NewGuid();
        var provider2 = Guid.NewGuid();

        var leagues = new List<ProviderLeague>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider1,
                ProviderSlug = "england/premier-league",
                ProviderName = "Premier League",
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider1,
                ProviderSlug = "spain/la-liga",
                ProviderName = "La Liga",
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider2,
                ProviderSlug = "germany/bundesliga",
                ProviderName = "Bundesliga",
                IsImported = false
            }
        };

        await _context.ProviderLeagues.AddRangeAsync(leagues);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProviderIdAsync(provider1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.Equal(provider1, l.ProviderId));
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByProviderSlugAsync_ReturnsCorrectLeague()
    {
        // Arrange
        var leagues = new List<ProviderLeague>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "england/premier-league",
                ProviderName = "Premier League",
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "spain/la-liga",
                ProviderName = "La Liga",
                IsImported = false
            }
        };

        await _context.ProviderLeagues.AddRangeAsync(leagues);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByProviderSlugAsync(_providerId, "spain/la-liga");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("La Liga", result.ProviderName);
        Assert.Equal("spain/la-liga", result.ProviderSlug);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByProviderSlugAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByProviderSlugAsync(_providerId, "unknown/league");

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task CreateAsync_ValidLeague_AddsLeague()
    {
        // Arrange
        var league = new ProviderLeague
        {
            ProviderId = _providerId,
            ProviderSlug = "italy/serie-a",
            ProviderName = "Serie A",
            DisplayName = "Italian Serie A",
            IsBettable = true,
            Priority = 2,
            IsImported = false,
            MappingStatus = MappingStatus.AutoMapped
        };

        // Act
        var result = await _repository.CreateAsync(league);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.ProviderLeagues.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Serie A", saved.ProviderName);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task UpdateAsync_ExistingLeague_UpdatesLeague()
    {
        // Arrange
        var league = new ProviderLeague
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            ProviderSlug = "france/ligue-1",
            ProviderName = "Ligue 1",
            DisplayName = "French Ligue 1",
            Priority = 3,
            IsImported = false
        };

        await _context.ProviderLeagues.AddAsync(league);
        await _context.SaveChangesAsync();

        // Act
        league.DisplayName = "Ligue 1 Updated";
        league.Priority = 1;
        league.IsImported = true;
        league.LeagueId = Guid.NewGuid();
        await _repository.UpdateAsync(league);

        // Assert
        var updated = await _context.ProviderLeagues.FindAsync(league.Id);
        Assert.NotNull(updated);
        Assert.Equal("Ligue 1 Updated", updated.DisplayName);
        Assert.Equal(1, updated.Priority);
        Assert.True(updated.IsImported);
        Assert.NotNull(updated.LeagueId);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetUnimportedAsync_ReturnsOnlyUnimportedLeagues()
    {
        // Arrange
        var leagues = new List<ProviderLeague>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "england/premier-league",
                ProviderName = "Premier League",
                IsImported = true,
                LeagueId = Guid.NewGuid()
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "spain/la-liga",
                ProviderName = "La Liga",
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "germany/bundesliga",
                ProviderName = "Bundesliga",
                IsImported = false
            }
        };

        await _context.ProviderLeagues.AddRangeAsync(leagues);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnimportedAsync(_providerId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.False(l.IsImported));
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetAllAsync_ReturnsAllLeagues()
    {
        // Arrange
        var leagues = new List<ProviderLeague>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "league1",
                ProviderName = "League 1",
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "league2",
                ProviderName = "League 2",
                IsImported = true
            }
        };

        await _context.ProviderLeagues.AddRangeAsync(leagues);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
