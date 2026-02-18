using Microsoft.EntityFrameworkCore;
using Sazkomat.Data.Data;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Tests.Helpers;

namespace Sazkomat.Tests.Data;

public class SyncJobRepositoryTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly SyncJobRepository _repository;
    private readonly Guid _providerId;

    public SyncJobRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _repository = new SyncJobRepository(_context, TestHelpers.CreateMockLogger<SyncJobRepository>());
        _providerId = Guid.NewGuid();
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var job = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending,
            Priority = 1
        };

        await _context.SyncJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(SyncJobType.Scan, result.Type);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByIdAsync_NonExistingJob_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task CreateAsync_ValidJob_AddsJob()
    {
        // Arrange
        var job = new SyncJob
        {
            ProviderId = _providerId,
            Type = SyncJobType.Import,
            EntityType = SyncEntityType.Leagues,
            Status = SyncJobStatus.Pending,
            Priority = 2
        };

        // Act
        var result = await _repository.CreateAsync(job);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.SyncJobs.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(SyncJobType.Import, saved.Type);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task UpdateAsync_ExistingJob_UpdatesJob()
    {
        // Arrange
        var job = new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Pending,
            Priority = 1
        };

        await _context.SyncJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Act
        job.Status = SyncJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        // Assert
        var updated = await _context.SyncJobs.FindAsync(job.Id);
        Assert.NotNull(updated);
        Assert.Equal(SyncJobStatus.Running, updated.Status);
        Assert.NotNull(updated.StartedAt);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetRecentJobsAsync_ReturnsJobsInDescendingOrder()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        var jobs = new List<SyncJob>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Completed,
                Priority = 1,
                CreatedAt = baseTime.AddMinutes(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Import,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Running,
                Priority = 2,
                CreatedAt = baseTime.AddMinutes(-15)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.LiveUpdate,
                EntityType = SyncEntityType.Rounds,
                Status = SyncJobStatus.Pending,
                Priority = 3,
                CreatedAt = baseTime
            }
        };

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRecentJobsAsync(_providerId, 10);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(jobs[2].Id, result[0].Id); // Most recent first
        Assert.Equal(jobs[1].Id, result[1].Id);
        Assert.Equal(jobs[0].Id, result[2].Id);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetRecentJobsAsync_LimitsResults()
    {
        // Arrange
        var jobs = Enumerable.Range(0, 15).Select(i => new SyncJob
        {
            Id = Guid.NewGuid(),
            ProviderId = _providerId,
            Type = SyncJobType.Scan,
            EntityType = SyncEntityType.Countries,
            Status = SyncJobStatus.Completed,
            Priority = 1,
            CreatedAt = DateTime.UtcNow.AddMinutes(-i)
        }).ToList();

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRecentJobsAsync(_providerId, 10);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetRecentJobsAsync_FiltersByProvider()
    {
        // Arrange
        var provider1 = Guid.NewGuid();
        var provider2 = Guid.NewGuid();

        var jobs = new List<SyncJob>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider1,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Completed,
                Priority = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider2,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Completed,
                Priority = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = provider1,
                Type = SyncJobType.Import,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Running,
                Priority = 2
            }
        };

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRecentJobsAsync(provider1, 10);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, j => Assert.Equal(provider1, j.ProviderId));
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetPendingJobsAsync_ReturnsPendingJobsOrderedByPriority()
    {
        // Arrange
        var jobs = new List<SyncJob>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Pending,
                Priority = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Import,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Pending,
                Priority = 1 // Highest priority
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.LiveUpdate,
                EntityType = SyncEntityType.Rounds,
                Status = SyncJobStatus.Running, // Not pending
                Priority = 10
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Seasons,
                Status = SyncJobStatus.Pending,
                Priority = 2
            }
        };

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingJobsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Priority); // Highest priority first
        Assert.Equal(2, result[1].Priority);
        Assert.Equal(3, result[2].Priority);
        Assert.All(result, j => Assert.Equal(SyncJobStatus.Pending, j.Status));
    }

    [Fact(Skip = "GetJobsByTypeAsync method not implemented in ISyncJobRepository")]
    public async Task GetJobsByTypeAsync_FiltersCorrectly()
    {
        // Arrange
        var jobs = new List<SyncJob>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Completed,
                Priority = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Import,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Running,
                Priority = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Pending,
                Priority = 1
            }
        };

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act - Method not implemented
        // var result = await _repository.GetJobsByTypeAsync(_providerId, SyncJobType.Scan, 10);

        // Assert
        // Assert.Equal(2, result.Count);
        // Assert.All(result, j => Assert.Equal(SyncJobType.Scan, j.Type));
        await Task.CompletedTask;
    }

    [Trait("Category", "Fast")]
    [Trait("Type", "Repository")]
    [Fact]
    public async Task GetByStatusAsync_FiltersCorrectly()
    {
        // Arrange
        var jobs = new List<SyncJob>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Scan,
                EntityType = SyncEntityType.Countries,
                Status = SyncJobStatus.Completed,
                Priority = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.Import,
                EntityType = SyncEntityType.Leagues,
                Status = SyncJobStatus.Running,
                Priority = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProviderId = _providerId,
                Type = SyncJobType.LiveUpdate,
                EntityType = SyncEntityType.Rounds,
                Status = SyncJobStatus.Completed,
                Priority = 3
            }
        };

        await _context.SyncJobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(SyncJobStatus.Completed);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, j => Assert.Equal(SyncJobStatus.Completed, j.Status));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
