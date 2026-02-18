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

    // Unmatched leagues queue (for manual mapping)
    public DbSet<UnmatchedLeague> UnmatchedLeagues => Set<UnmatchedLeague>();

    // Unmatched countries queue (for manual mapping)
    public DbSet<UnmatchedCountry> UnmatchedCountries => Set<UnmatchedCountry>();

    // Scraper recipes for adaptive scraping
    public DbSet<ScraperRecipe> ScraperRecipes => Set<ScraperRecipe>();

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

        // Unmatched leagues queue
        modelBuilder.ApplyConfiguration(new UnmatchedLeagueConfiguration());

        // Unmatched countries queue
        modelBuilder.ApplyConfiguration(new UnmatchedCountryConfiguration());

        // Scraper recipes
        modelBuilder.ApplyConfiguration(new ScraperRecipeConfiguration());

        // Season from configuration schema (read-only, for cross-schema JOINs)
        modelBuilder.Entity<Season>(b =>
        {
            b.ToTable("seasons", "configuration", t => t.ExcludeFromMigrations());
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasColumnName("id");
            b.Property(s => s.Name).HasColumnName("name");
            b.Property(s => s.StartYear).HasColumnName("start_year");
            b.Property(s => s.EndYear).HasColumnName("end_year");
            b.Property(s => s.CreatedAt).HasColumnName("created_at");
            b.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            b.Ignore(s => s.LeagueSeasons);
        });
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
