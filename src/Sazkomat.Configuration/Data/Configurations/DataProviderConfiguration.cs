using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class DataProviderConfiguration : IEntityTypeConfiguration<DataProvider>
{
    public void Configure(EntityTypeBuilder<DataProvider> builder)
    {
        builder.ToTable("data_providers", "configuration");

        builder.HasKey(dp => dp.Id);

        builder.Property(dp => dp.Id)
            .HasColumnName("id");

        builder.Property(dp => dp.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(dp => dp.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(dp => dp.BaseUrl)
            .HasColumnName("base_url")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(dp => dp.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(dp => dp.Priority)
            .HasColumnName("priority")
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(dp => dp.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(dp => dp.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(dp => dp.CurrentSeasonPatterns)
            .HasColumnName("current_season_patterns")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(dp => dp.Credentials)
            .HasColumnName("credentials")
            .HasColumnType("jsonb");

        builder.Property(dp => dp.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb");

        builder.Property(dp => dp.ScanCapabilities)
            .HasColumnName("scan_capabilities")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{\"canScanCountries\":true,\"canScanLeagues\":true,\"canScanSeasons\":true}");

        builder.Property(dp => dp.HasLogo)
            .HasColumnName("has_logo")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(dp => dp.LogoUploadedAt)
            .HasColumnName("logo_uploaded_at");

        builder.Property(dp => dp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(dp => dp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(dp => dp.Code)
            .IsUnique()
            .HasDatabaseName("ix_data_providers_code");

        builder.HasIndex(dp => dp.IsActive)
            .HasDatabaseName("ix_data_providers_is_active");
    }
}
