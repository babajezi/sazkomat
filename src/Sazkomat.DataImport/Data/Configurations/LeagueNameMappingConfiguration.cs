using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data.Configurations;

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
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.ProviderLeagueName)
            .HasColumnName("provider_league_name")
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

        // Indexes for performance
        builder.HasIndex(m => new { m.ProviderCode, m.CountryCode, m.ProviderLeagueName, m.IsActive })
            .HasDatabaseName("ix_league_name_mappings_lookup");

        builder.HasIndex(m => m.ProviderCode)
            .HasDatabaseName("ix_league_name_mappings_provider_code");

        builder.HasIndex(m => m.CountryCode)
            .HasDatabaseName("ix_league_name_mappings_country_code");
    }
}
