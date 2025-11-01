using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Data.Configurations;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Data;

public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<LeagueSeason> LeagueSeasons => Set<LeagueSeason>();
    public DbSet<DataProvider> DataProviders => Set<DataProvider>();
    public DbSet<CountryProvider> CountryProviders => Set<CountryProvider>();
    public DbSet<LeagueProvider> LeagueProviders => Set<LeagueProvider>();
    public DbSet<SportProvider> SportProviders => Set<SportProvider>();
    public DbSet<SyncWorkflowState> SyncWorkflowStates => Set<SyncWorkflowState>();
    public DbSet<LogSettings> LogSettings => Set<LogSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SportConfiguration());
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueSeasonConfiguration());
        modelBuilder.ApplyConfiguration(new DataProviderConfiguration());
        modelBuilder.ApplyConfiguration(new CountryProviderConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueProviderConfiguration());
        modelBuilder.ApplyConfiguration(new SportProviderConfiguration());
        modelBuilder.ApplyConfiguration(new SyncWorkflowStateConfiguration());
        modelBuilder.ApplyConfiguration(new LogSettingsConfiguration());
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
