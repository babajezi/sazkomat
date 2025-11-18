using Moq;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Tests.Configuration;

public class SyncWorkflowServiceTests
{
    private readonly Mock<ISyncWorkflowStateRepository> _mockRepository;
    private readonly SyncWorkflowService _service;

    public SyncWorkflowServiceTests()
    {
        _mockRepository = new Mock<ISyncWorkflowStateRepository>();
        _service = new SyncWorkflowService(_mockRepository.Object);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task GetStateAsync_ReturnsState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = false,
            CountriesConfirmed = false,
            LeaguesSynced = false,
            LeaguesConfirmed = false,
            SeasonsSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.GetStateAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(state.Id, result.Value.Id);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task MarkCountriesSyncedAsync_UpdatesState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = false,
            CountriesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.MarkCountriesSyncedAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(state.CountriesSynced);
        Assert.NotNull(state.CountriesSyncedAt);
        _mockRepository.Verify(r => r.UpdateAsync(state), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ConfirmCountriesAsync_WhenNotSynced_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = false,
            CountriesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.ConfirmCountriesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("must be synced before confirmation", result.Error);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SyncWorkflowState>()), Times.Never);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ConfirmCountriesAsync_WhenSynced_UpdatesState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.ConfirmCountriesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(state.CountriesConfirmed);
        _mockRepository.Verify(r => r.UpdateAsync(state), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task MarkLeaguesSyncedAsync_UpdatesState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            LeaguesSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.MarkLeaguesSyncedAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(state.LeaguesSynced);
        Assert.NotNull(state.LeaguesSyncedAt);
        _mockRepository.Verify(r => r.UpdateAsync(state), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ConfirmLeaguesAsync_WhenNotSynced_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            LeaguesSynced = false,
            LeaguesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.ConfirmLeaguesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("must be synced before confirmation", result.Error);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SyncWorkflowState>()), Times.Never);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ConfirmLeaguesAsync_WhenSynced_UpdatesState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            LeaguesSynced = true,
            LeaguesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.ConfirmLeaguesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(state.LeaguesConfirmed);
        _mockRepository.Verify(r => r.UpdateAsync(state), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task MarkSeasonsSyncedAsync_UpdatesState()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            SeasonsSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.MarkSeasonsSyncedAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(state.SeasonsSynced);
        Assert.NotNull(state.SeasonsSyncedAt);
        _mockRepository.Verify(r => r.UpdateAsync(state), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task ResetWorkflowAsync_CallsRepositoryReset()
    {
        // Act
        var result = await _service.ResetWorkflowAsync();

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.ResetAsync(), Times.Once);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncCountriesAsync_WhenNotSynced_ReturnsSuccess()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncCountriesAsync();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncCountriesAsync_WhenAlreadySynced_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncCountriesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already been synced", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncLeaguesAsync_WhenCountriesNotConfirmed_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncLeaguesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Countries must be confirmed", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncLeaguesAsync_WhenLeaguesAlreadySynced_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = true,
            LeaguesSynced = true
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncLeaguesAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already been synced", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncLeaguesAsync_WhenValid_ReturnsSuccess()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = true,
            LeaguesSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncLeaguesAsync();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncSeasonsAsync_WhenLeaguesNotConfirmed_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = true,
            LeaguesSynced = true,
            LeaguesConfirmed = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncSeasonsAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Leagues must be confirmed", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncSeasonsAsync_WhenSeasonsAlreadySynced_ReturnsFailure()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = true,
            LeaguesSynced = true,
            LeaguesConfirmed = true,
            SeasonsSynced = true
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncSeasonsAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already been synced", result.Error);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task CanSyncSeasonsAsync_WhenValid_ReturnsSuccess()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = true,
            CountriesConfirmed = true,
            LeaguesSynced = true,
            LeaguesConfirmed = true,
            SeasonsSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act
        var result = await _service.CanSyncSeasonsAsync();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Trait("Category", "Slow")]
    [Trait("Type", "Service")]
    [Fact]
    public async Task WorkflowProgression_FullFlow_WorksCorrectly()
    {
        // Arrange
        var state = new SyncWorkflowState
        {
            Id = Guid.NewGuid(),
            CountriesSynced = false,
            CountriesConfirmed = false,
            LeaguesSynced = false,
            LeaguesConfirmed = false,
            SeasonsSynced = false
        };

        _mockRepository.Setup(r => r.GetOrCreateAsync())
            .ReturnsAsync(state);

        // Act & Assert - Step 1: Sync countries
        var canSyncCountries = await _service.CanSyncCountriesAsync();
        Assert.True(canSyncCountries.IsSuccess);

        await _service.MarkCountriesSyncedAsync();
        Assert.True(state.CountriesSynced);

        // Can't sync leagues yet (countries not confirmed)
        var canSyncLeagues1 = await _service.CanSyncLeaguesAsync();
        Assert.False(canSyncLeagues1.IsSuccess);

        // Step 2: Confirm countries
        await _service.ConfirmCountriesAsync();
        Assert.True(state.CountriesConfirmed);

        // Now can sync leagues
        var canSyncLeagues2 = await _service.CanSyncLeaguesAsync();
        Assert.True(canSyncLeagues2.IsSuccess);

        // Step 3: Sync leagues
        await _service.MarkLeaguesSyncedAsync();
        Assert.True(state.LeaguesSynced);

        // Can't sync seasons yet (leagues not confirmed)
        var canSyncSeasons1 = await _service.CanSyncSeasonsAsync();
        Assert.False(canSyncSeasons1.IsSuccess);

        // Step 4: Confirm leagues
        await _service.ConfirmLeaguesAsync();
        Assert.True(state.LeaguesConfirmed);

        // Now can sync seasons
        var canSyncSeasons2 = await _service.CanSyncSeasonsAsync();
        Assert.True(canSyncSeasons2.IsSuccess);

        // Step 5: Sync seasons
        await _service.MarkSeasonsSyncedAsync();
        Assert.True(state.SeasonsSynced);
    }
}
