using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;
using Sazkomat.DataImport.Data.Configurations;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data;

public class DataImportDbContext : DbContext
{
    public DataImportDbContext(DbContextOptions<DataImportDbContext> options)
        : base(options)
    {
    }

    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    // Provider cache entities
    public DbSet<ProviderCountry> ProviderCountries => Set<ProviderCountry>();
    public DbSet<ProviderLeague> ProviderLeagues => Set<ProviderLeague>();
    public DbSet<ProviderSeason> ProviderSeasons => Set<ProviderSeason>();

    // Sync job queue
    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();

    // League name mappings
    public DbSet<LeagueNameMapping> LeagueNameMappings => Set<LeagueNameMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Original entities
        modelBuilder.ApplyConfiguration(new RoundConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());

        // Provider cache entities
        modelBuilder.ApplyConfiguration(new ProviderCountryConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderLeagueConfiguration());
        modelBuilder.ApplyConfiguration(new ProviderSeasonConfiguration());

        // Sync job queue
        modelBuilder.ApplyConfiguration(new SyncJobConfiguration());

        // League name mappings
        modelBuilder.ApplyConfiguration(new LeagueNameMappingConfiguration());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
