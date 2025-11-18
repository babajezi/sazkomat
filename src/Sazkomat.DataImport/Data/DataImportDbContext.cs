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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Temporarily suppress pending model changes warning
        // This allows the app to start while we work on the migration
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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

    // Name mappings
    public DbSet<LeagueNameMapping> LeagueNameMappings => Set<LeagueNameMapping>();
    public DbSet<CountryNameMapping> CountryNameMappings => Set<CountryNameMapping>();

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

        // Name mappings
        modelBuilder.ApplyConfiguration(new LeagueNameMappingConfiguration());
        modelBuilder.ApplyConfiguration(new CountryNameMappingConfiguration());
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
