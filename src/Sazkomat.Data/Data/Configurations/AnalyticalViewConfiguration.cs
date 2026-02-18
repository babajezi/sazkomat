using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class AnalyticalViewConfiguration : IEntityTypeConfiguration<AnalyticalView>
{
    public void Configure(EntityTypeBuilder<AnalyticalView> builder)
    {
        builder.ToTable("analytical_views", "data_import");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(v => v.SpecJson)
            .HasColumnName("spec_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(v => v.Tags)
            .HasColumnName("tags")
            .HasMaxLength(500);

        builder.Property(v => v.IsFavorite)
            .HasColumnName("is_favorite")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.ExecutionCount)
            .HasColumnName("execution_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(v => v.LastExecutedAt)
            .HasColumnName("last_executed_at");

        builder.Property(v => v.LastExecutionMs)
            .HasColumnName("last_execution_ms");

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(v => v.Name)
            .HasDatabaseName("ix_analytical_views_name");

        builder.HasIndex(v => v.IsFavorite)
            .HasDatabaseName("ix_analytical_views_is_favorite");

        builder.HasIndex(v => v.LastExecutedAt)
            .HasDatabaseName("ix_analytical_views_last_executed_at");
    }
}
