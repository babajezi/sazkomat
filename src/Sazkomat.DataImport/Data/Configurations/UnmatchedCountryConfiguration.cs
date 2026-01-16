using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data.Configurations;

public class UnmatchedCountryConfiguration : IEntityTypeConfiguration<UnmatchedCountry>
{
    public void Configure(EntityTypeBuilder<UnmatchedCountry> builder)
    {
        builder.ToTable("unmatched_countries", "data_import");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(u => u.ProviderCountryId)
            .HasColumnName("provider_country_id")
            .HasMaxLength(100);

        builder.Property(u => u.ProviderCountryName)
            .HasColumnName("provider_country_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.ProviderSlug)
            .HasColumnName("provider_slug")
            .HasMaxLength(200);

        builder.Property(u => u.ScrapedAt)
            .HasColumnName("scraped_at")
            .IsRequired();

        // Resolution tracking
        builder.Property(u => u.IsResolved)
            .HasColumnName("is_resolved")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.ResolutionType)
            .HasColumnName("resolution_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.ResolvedCountryId)
            .HasColumnName("resolved_country_id");

        builder.Property(u => u.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(u => u.ResolutionNotes)
            .HasColumnName("resolution_notes")
            .HasMaxLength(500);

        // Timestamps
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign key relationships
        // Note: Provider and ResolvedCountry are in configuration schema, managed by ConfigurationDbContext
        // We only define the FK columns here, navigation is handled manually
        builder.Ignore(u => u.Provider);
        builder.Ignore(u => u.ResolvedCountry);

        // Indexes
        builder.HasIndex(u => u.ProviderId)
            .HasDatabaseName("ix_unmatched_countries_provider_id");

        builder.HasIndex(u => u.IsResolved)
            .HasDatabaseName("ix_unmatched_countries_is_resolved");

        // Unique constraint - same country from same provider should not be duplicated
        builder.HasIndex(u => new { u.ProviderId, u.ProviderCountryName })
            .IsUnique()
            .HasDatabaseName("ix_unmatched_countries_unique");
    }
}
