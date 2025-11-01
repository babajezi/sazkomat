using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Tests.DataImport;

public class ImportJobRepositoryTests : IDisposable
{
    private readonly DataImportDbContext _context;
    private readonly ImportJobRepository _repository;
    private readonly Guid _testLeagueId;

    public ImportJobRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataImportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataImportDbContext(options);
        _repository = new ImportJobRepository(_context);
        _testLeagueId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            Seasons = new List<string> { "2023/2024", "2022/2023" },
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData
            {
                TotalSeasons = 2,
                ProcessedSeasons = new List<string>(),
                ProcessedRounds = 0,
                Errors = new List<string>()
            }
        };

        await _context.ImportJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(ImportJobStatus.Pending, result.Status);
        Assert.Equal(2, result.Seasons.Count);
    }

    [Fact]
    public async Task CreateAsync_ValidJob_AddsJob()
    {
        // Arrange
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            Seasons = new List<string> { "2023/2024" },
            IncludeWithoutOdds = true,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData
            {
                TotalSeasons = 1,
                ProcessedSeasons = new List<string>(),
                ProcessedRounds = 0,
                Errors = new List<string>()
            }
        };

        // Act
        await _repository.CreateAsync(job);
        var result = await _context.ImportJobs.FindAsync(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Single(result.Seasons);
    }

    [Fact]
    public async Task UpdateAsync_ExistingJob_UpdatesJob()
    {
        // Arrange
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = _testLeagueId,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            Seasons = new List<string> { "2023/2024" },
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData
            {
                TotalSeasons = 1,
                ProcessedSeasons = new List<string>(),
                ProcessedRounds = 0,
                Errors = new List<string>()
            }
        };

        await _context.ImportJobs.AddAsync(job);
        await _context.SaveChangesAsync();

        // Act
        job.Status = ImportJobStatus.Running;
        job.Progress.ProcessedSeasons.Add("2023/2024");
        await _repository.UpdateAsync(job);

        var result = await _context.ImportJobs.FindAsync(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ImportJobStatus.Running, result.Status);
        Assert.Single(result.Progress.ProcessedSeasons);
    }

    [Fact]
    public async Task GetByLeagueIdAsync_FiltersByLeague()
    {
        // Arrange
        var leagueId1 = Guid.NewGuid();
        var leagueId2 = Guid.NewGuid();

        var job1 = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId1,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            Seasons = new List<string> { "2023/2024" },
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData()
        };

        var job2 = new ImportJob
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId2,
            Type = ImportJobType.Historical,
            Status = ImportJobStatus.Pending,
            Seasons = new List<string> { "2023/2024" },
            IncludeWithoutOdds = false,
            StartedAt = DateTime.UtcNow,
            Progress = new ImportProgressData()
        };

        await _context.ImportJobs.AddRangeAsync(job1, job2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByLeagueIdAsync(leagueId1);

        // Assert
        Assert.Single(result);
        Assert.Equal(leagueId1, result[0].LeagueId);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
