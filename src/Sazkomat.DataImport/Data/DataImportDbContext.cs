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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoundConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());
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
