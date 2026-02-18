using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class LeagueNameMappingConfiguration : IEntityTypeConfiguration<LeagueNameMapping>
{
    public void Configure(EntityTypeBuilder<LeagueNameMapping> builder)
    {
        builder.ToTable("league_name_mappings", "data_import");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(50)  // Country slugs can be long (e.g., "switzerland", "north-central-america")
            .IsRequired();

        builder.Property(m => m.ProviderLeagueName)
            .HasColumnName("provider_league_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.NormalizedProviderLeagueName)
            .HasColumnName("normalized_provider_league_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.BetExplorerSlug)
            .HasColumnName("betexplorer_slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(m => m.Priority)
            .HasColumnName("priority")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(m => m.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.Property(m => m.UsageCount)
            .HasColumnName("usage_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.LastProviderLeagueId)
            .HasColumnName("last_provider_league_id");

        // Indexes for performance
        builder.HasIndex(m => new { m.ProviderCode, m.CountryCode, m.ProviderLeagueName, m.IsActive })
            .HasDatabaseName("ix_league_name_mappings_lookup");

        builder.HasIndex(m => m.ProviderCode)
            .HasDatabaseName("ix_league_name_mappings_provider_code");

        builder.HasIndex(m => m.CountryCode)
            .HasDatabaseName("ix_league_name_mappings_country_code");

        // Index for normalized lookup (used for global rule fallback)
        builder.HasIndex(m => new { m.CountryCode, m.NormalizedProviderLeagueName, m.IsActive })
            .HasDatabaseName("ix_league_name_mappings_normalized_lookup");

        // Computed property IsGlobal is not mapped to database
        builder.Ignore(m => m.IsGlobal);
    }
}
