using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class CountryProviderConfiguration : IEntityTypeConfiguration<CountryProvider>
{
    public void Configure(EntityTypeBuilder<CountryProvider> builder)
    {
        builder.ToTable("country_providers", "configuration");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Id)
            .HasColumnName("id");

        builder.Property(cp => cp.CountryId)
            .HasColumnName("country_id")
            .IsRequired();

        builder.Property(cp => cp.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(cp => cp.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(cp => cp.ProviderName)
            .HasColumnName("provider_name")
            .HasMaxLength(200);

        builder.Property(cp => cp.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(cp => cp.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(cp => cp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(cp => cp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign Keys
        builder.HasOne(cp => cp.Country)
            .WithMany(c => c.CountryProviders)
            .HasForeignKey(cp => cp.CountryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Provider)
            .WithMany(p => p.CountryProviders)
            .HasForeignKey(cp => cp.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(cp => new { cp.CountryId, cp.ProviderId })
            .IsUnique()
            .HasDatabaseName("ix_country_providers_country_provider");

        builder.HasIndex(cp => cp.ProviderCode)
            .HasDatabaseName("ix_country_providers_provider_code");

        builder.HasIndex(cp => cp.IsActive)
            .HasDatabaseName("ix_country_providers_is_active");
    }
}
