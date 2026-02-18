using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;
using System.Text.Json;

namespace Sazkomat.Data.Data.Configurations;

public class SyncJobConfiguration : IEntityTypeConfiguration<SyncJob>
{
    public void Configure(EntityTypeBuilder<SyncJob> builder)
    {
        builder.ToTable("sync_jobs", "data_import");

        builder.HasKey(sj => sj.Id);

        builder.Property(sj => sj.Id).HasColumnName("id");
        builder.Property(sj => sj.ProviderId).HasColumnName("provider_id").IsRequired();

        builder.Property(sj => sj.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(sj => sj.EntityType)
            .HasColumnName("entity_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(sj => sj.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(sj => sj.Priority).HasColumnName("priority").IsRequired();
        builder.Property(sj => sj.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(sj => sj.CompletedAt).HasColumnName("completed_at");
        builder.Property(sj => sj.ScheduledFor).HasColumnName("scheduled_for");
        builder.Property(sj => sj.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(sj => sj.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(sj => sj.MaxRetries).HasColumnName("max_retries").IsRequired();

        builder.Property(sj => sj.ProgressData)
            .HasColumnName("progress_data")
            .HasColumnType("jsonb");

        builder.Property(sj => sj.CountryIds)
            .HasColumnName("country_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(sj => sj.LeagueIds)
            .HasColumnName("league_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(sj => sj.SeasonIds)
            .HasColumnName("season_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(sj => sj.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(sj => sj.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore navigation properties
        builder.Ignore(sj => sj.Provider);

        // Indexes
        builder.HasIndex(sj => sj.ProviderId);
        builder.HasIndex(sj => sj.Type);
        builder.HasIndex(sj => sj.EntityType);
        builder.HasIndex(sj => sj.Status);
        builder.HasIndex(sj => sj.Priority);
        builder.HasIndex(sj => sj.ScheduledFor);
        builder.HasIndex(sj => new { sj.Status, sj.Priority });  // Composite for queue processing
    }
}
