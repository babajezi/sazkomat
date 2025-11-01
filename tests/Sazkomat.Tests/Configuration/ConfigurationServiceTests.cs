using Moq;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Tests.Configuration;

public class ConfigurationServiceTests
{
    private readonly Mock<ILeagueRepository> _mockLeagueRepository;
    private readonly Mock<ISportRepository> _mockSportRepository;
    private readonly Mock<ICountryRepository> _mockCountryRepository;
    private readonly ConfigurationService _service;

    public ConfigurationServiceTests()
    {
        _mockLeagueRepository = new Mock<ILeagueRepository>();
        _mockSportRepository = new Mock<ISportRepository>();
        _mockCountryRepository = new Mock<ICountryRepository>();
        _service = new ConfigurationService(
            _mockSportRepository.Object,
            _mockCountryRepository.Object,
            _mockLeagueRepository.Object
        );
    }

    [Fact]
    public async Task CreateLeagueAsync_ValidRequest_CreatesLeague()
    {
        // Arrange
        var sportId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        var sport = new Sport
        {
            Id = sportId,
            Name = "Football",
            Code = "FOOT",
            IsActive = true
        };

        var country = new Country
        {
            Id = countryId,
            Name = "England",
            Code = "ENG",
            FlagEmoji = "🏴"
        };

        var request = new CreateLeagueRequest(
            SportId: sportId,
            CountryId: countryId,
            Name: "Premier League",
            BetExplorerSlug: "england/premier-league",
            IsBettable: true,
            Priority: 1,
            Notes: "Test league"
        );

        _mockSportRepository.Setup(r => r.GetByIdAsync(sportId))
            .ReturnsAsync(sport);
        _mockCountryRepository.Setup(r => r.GetByIdAsync(countryId))
            .ReturnsAsync(country);
        _mockLeagueRepository.Setup(r => r.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<League>());
        _mockLeagueRepository.Setup(r => r.CreateAsync(It.IsAny<League>()))
            .ReturnsAsync((League l) => l);

        // Act
        var result = await _service.CreateLeagueAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Premier League", result.Value.Name);
        _mockLeagueRepository.Verify(r => r.CreateAsync(It.IsAny<League>()), Times.Once);
    }

    [Fact]
    public async Task CreateLeagueAsync_InvalidSport_ReturnsFailure()
    {
        // Arrange
        var sportId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        var request = new CreateLeagueRequest(
            SportId: sportId,
            CountryId: countryId,
            Name: "Premier League",
            BetExplorerSlug: "england/premier-league",
            IsBettable: true,
            Priority: 1
        );

        _mockSportRepository.Setup(r => r.GetByIdAsync(sportId))
            .ReturnsAsync((Sport?)null);

        // Act
        var result = await _service.CreateLeagueAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Sport", result.Error);
        _mockLeagueRepository.Verify(r => r.CreateAsync(It.IsAny<League>()), Times.Never);
    }

    [Fact]
    public async Task CreateLeagueAsync_InvalidCountry_ReturnsFailure()
    {
        // Arrange
        var sportId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        var sport = new Sport
        {
            Id = sportId,
            Name = "Football",
            Code = "FOOT",
            IsActive = true
        };

        var request = new CreateLeagueRequest(
            SportId: sportId,
            CountryId: countryId,
            Name: "Premier League",
            BetExplorerSlug: "england/premier-league",
            IsBettable: true,
            Priority: 1
        );

        _mockSportRepository.Setup(r => r.GetByIdAsync(sportId))
            .ReturnsAsync(sport);
        _mockCountryRepository.Setup(r => r.GetByIdAsync(countryId))
            .ReturnsAsync((Country?)null);

        // Act
        var result = await _service.CreateLeagueAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Country", result.Error);
        _mockLeagueRepository.Verify(r => r.CreateAsync(It.IsAny<League>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLeagueAsync_ExistingLeague_UpdatesLeague()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var league = new League
        {
            Id = leagueId,
            Name = "Premier League",
            DisplayName = "Premier League (England)",
            BetExplorerSlug = "england/premier-league",
            SportId = Guid.NewGuid(),
            CountryId = Guid.NewGuid(),
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 1,
            Country = new Country
            {
                Id = Guid.NewGuid(),
                Name = "England",
                Code = "ENG",
                FlagEmoji = "🏴"
            }
        };

        var request = new UpdateLeagueRequest
        {
            IsSyncEnabled = true,
            Priority = 5
        };

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(league);
        _mockLeagueRepository.Setup(r => r.UpdateAsync(It.IsAny<League>()))
            .ReturnsAsync((League l) => l);

        // Act
        var result = await _service.UpdateLeagueAsync(leagueId, request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockLeagueRepository.Verify(r => r.UpdateAsync(It.IsAny<League>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLeagueAsync_NonExistingLeague_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var request = new UpdateLeagueRequest
        {
            IsSyncEnabled = true
        };

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync((League?)null);

        // Act
        var result = await _service.UpdateLeagueAsync(leagueId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
        _mockLeagueRepository.Verify(r => r.UpdateAsync(It.IsAny<League>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLeagueAsync_ExistingLeague_DeletesLeague()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var league = new League
        {
            Id = leagueId,
            Name = "Premier League",
            DisplayName = "Premier League (England)",
            BetExplorerSlug = "england/premier-league",
            SportId = Guid.NewGuid(),
            CountryId = Guid.NewGuid(),
            IsSyncEnabled = true,
            IsBettable = true,
            Priority = 1
        };

        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync(league);
        _mockLeagueRepository.Setup(r => r.DeleteAsync(leagueId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteLeagueAsync(leagueId);

        // Assert
        Assert.True(result.IsSuccess);
        _mockLeagueRepository.Verify(r => r.DeleteAsync(leagueId), Times.Once);
    }

    [Fact]
    public async Task DeleteLeagueAsync_NonExistingLeague_ReturnsFailure()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        _mockLeagueRepository.Setup(r => r.GetByIdAsync(leagueId))
            .ReturnsAsync((League?)null);

        // Act
        var result = await _service.DeleteLeagueAsync(leagueId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
        _mockLeagueRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
