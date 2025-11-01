using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class SportProviderConfiguration : IEntityTypeConfiguration<SportProvider>
{
    public void Configure(EntityTypeBuilder<SportProvider> builder)
    {
        builder.ToTable("sport_providers", "configuration");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .HasColumnName("id");

        builder.Property(sp => sp.SportId)
            .HasColumnName("sport_id")
            .IsRequired();

        builder.Property(sp => sp.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(sp => sp.ProviderCode)
            .HasColumnName("provider_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sp => sp.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(sp => sp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign Keys
        builder.HasOne(sp => sp.Sport)
            .WithMany(s => s.SportProviders)
            .HasForeignKey(sp => sp.SportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.Provider)
            .WithMany(p => p.SportProviders)
            .HasForeignKey(sp => sp.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sp => new { sp.SportId, sp.ProviderId })
            .IsUnique()
            .HasDatabaseName("ix_sport_providers_sport_provider");

        builder.HasIndex(sp => sp.ProviderCode)
            .HasDatabaseName("ix_sport_providers_provider_code");

        builder.HasIndex(sp => sp.IsActive)
            .HasDatabaseName("ix_sport_providers_is_active");
    }
}
