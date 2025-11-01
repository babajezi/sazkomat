using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;
using System.Text.Json;

namespace Sazkomat.DataImport.Data.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("import_jobs", "data_import");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.LeagueId).HasColumnName("league_id").IsRequired();
        builder.Property(j => j.ProviderId).HasColumnName("provider_id").IsRequired();

        builder.Property(j => j.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(j => j.SeasonIds)
            .HasColumnName("season_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(j => j.IncludeWithoutOdds).HasColumnName("include_without_odds").IsRequired();
        builder.Property(j => j.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        builder.Property(j => j.Progress)
            .HasColumnName("progress")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<ImportProgressData>(v, (JsonSerializerOptions?)null) ?? new ImportProgressData())
            .IsRequired();

        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore navigation property (League is managed by ConfigurationDbContext)
        builder.Ignore(j => j.League);

        // Indexes
        builder.HasIndex(j => j.LeagueId);
        builder.HasIndex(j => j.ProviderId);
        builder.HasIndex(j => j.Status);
    }
}
