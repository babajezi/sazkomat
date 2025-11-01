using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class LogSettingsConfiguration : IEntityTypeConfiguration<LogSettings>
{
    public void Configure(EntityTypeBuilder<LogSettings> builder)
    {
        builder.ToTable("log_settings", "configuration");

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("category");

        builder.Property(ls => ls.SubCategory)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("sub_category");

        builder.Property(ls => ls.LogPath)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("log_path");

        builder.Property(ls => ls.LogLevel)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("log_level")
            .HasDefaultValue("Information");

        builder.Property(ls => ls.IsEnabled)
            .IsRequired()
            .HasColumnName("is_enabled")
            .HasDefaultValue(true);

        builder.Property(ls => ls.RetentionDays)
            .IsRequired()
            .HasColumnName("retention_days")
            .HasDefaultValue(30);

        builder.Property(ls => ls.MaxFileSizeBytes)
            .IsRequired()
            .HasColumnName("max_file_size_bytes")
            .HasDefaultValue(104857600L); // 100 MB

        builder.Property(ls => ls.OutputTemplate)
            .HasMaxLength(1000)
            .HasColumnName("output_template");

        builder.Property(ls => ls.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        // Composite unique index on Category + SubCategory
        builder.HasIndex(ls => new { ls.Category, ls.SubCategory })
            .IsUnique()
            .HasDatabaseName("ix_log_settings_category_subcategory");

        // Index for quick lookups by category
        builder.HasIndex(ls => ls.Category)
            .HasDatabaseName("ix_log_settings_category");
    }
}
