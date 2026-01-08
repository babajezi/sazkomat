using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

/// <summary>
/// EF Core configuration for ApplicationUser entity
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Table name
        builder.ToTable("AspNetUsers", "configuration");

        // Custom properties
        builder.Property(u => u.LanguagePreference)
            .HasColumnName("language_preference")
            .IsRequired()
            .HasDefaultValue(Core.Enums.LanguagePreference.Czech);

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.IsApproved)
            .HasColumnName("is_approved")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(u => u.ApprovedBy)
            .HasColumnName("approved_by")
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.CreatedAt);

        builder.HasIndex(u => u.IsApproved);
    }
}
