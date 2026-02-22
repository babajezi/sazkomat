using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class StrategyScreeningConfiguration : IEntityTypeConfiguration<StrategyScreening>
{
    public void Configure(EntityTypeBuilder<StrategyScreening> builder)
    {
        builder.ToTable("strategy_screenings", "data_import");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.StrategyType)
            .HasColumnName("strategy_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ParametersJson)
            .HasColumnName("parameters_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.ResultJson)
            .HasColumnName("result_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.RoundsAnalyzed)
            .HasColumnName("rounds_analyzed")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.CalculatedAt)
            .HasColumnName("calculated_at")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.StrategyType)
            .HasDatabaseName("ix_strategy_screenings_strategy_type");
    }
}
