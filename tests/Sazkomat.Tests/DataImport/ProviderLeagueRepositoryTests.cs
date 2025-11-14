using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Tests.DataImport;

public class ProviderLeagueRepositoryTests : IDisposable
{
    private readonly DataImportDbContext _context;
    private readonly ProviderLeagueRepository _repository;
    private readonly Guid _providerId;

    public ProviderLeagueRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataImportDbContext(options);
        _repository = new ProviderLeagueRepository(_context);
        _providerId = Guid.NewGuid();
    }

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

    [Fact]
    public async Task GetByProviderSlugAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByProviderSlugAsync(_providerId, "unknown/league");

        // Assert
        Assert.Null(result);
    }

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
            MappingStatus = MappingStatus.Mapped
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

    [Fact]
    public async Task GetByMappingStatusAsync_FiltersCorrectly()
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
                MappingStatus = MappingStatus.Mapped,
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "unknown/league",
                ProviderName = "Unknown League",
                MappingStatus = MappingStatus.Unmapped,
                IsImported = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                ProviderSlug = "pending/league",
                ProviderName = "Pending League",
                MappingStatus = MappingStatus.PendingReview,
                IsImported = false
            }
        };

        await _context.ProviderLeagues.AddRangeAsync(leagues);
        await _context.SaveChangesAsync();

        // Act
        var mapped = await _repository.GetByMappingStatusAsync(_providerId, MappingStatus.Mapped);
        var unmapped = await _repository.GetByMappingStatusAsync(_providerId, MappingStatus.Unmapped);

        // Assert
        Assert.Single(mapped);
        Assert.Equal("Premier League", mapped[0].ProviderName);
        Assert.Single(unmapped);
        Assert.Equal("Unknown League", unmapped[0].ProviderName);
    }

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
