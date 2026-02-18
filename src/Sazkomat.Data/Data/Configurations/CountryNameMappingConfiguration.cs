using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class CountryNameMappingConfiguration : IEntityTypeConfiguration<CountryNameMapping>
{
    public void Configure(EntityTypeBuilder<CountryNameMapping> builder)
    {
        builder.ToTable("country_name_mappings", "data_import");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ProviderCountryName)
            .HasColumnName("provider_country_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.BetExplorerCode)
            .HasColumnName("betexplorer_code")
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

        builder.Property(m => m.LastProviderCountryId)
            .HasColumnName("last_provider_country_id");

        builder.Property(m => m.MatchType)
            .HasColumnName("match_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("substring");

        builder.Property(m => m.IsCaseSensitive)
            .HasColumnName("is_case_sensitive")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsSpecialCase)
            .HasColumnName("is_special_case")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.LocalizedName)
            .HasColumnName("localized_name")
            .HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(m => new { m.ProviderCode, m.ProviderCountryName, m.IsActive })
            .HasDatabaseName("ix_country_name_mappings_lookup");

        builder.HasIndex(m => m.ProviderCode)
            .HasDatabaseName("ix_country_name_mappings_provider_code");

        // Index for special cases - checked first during pattern matching
        builder.HasIndex(m => new { m.ProviderCode, m.IsSpecialCase, m.IsActive, m.Priority })
            .HasDatabaseName("ix_country_name_mappings_special_cases");
    }
}
