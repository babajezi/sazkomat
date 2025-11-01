using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class LeagueProviderConfiguration : IEntityTypeConfiguration<LeagueProvider>
{
    public void Configure(EntityTypeBuilder<LeagueProvider> builder)
    {
        builder.ToTable("league_providers", "configuration");

        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.Id)
            .HasColumnName("id");

        builder.Property(lp => lp.LeagueId)
            .HasColumnName("league_id")
            .IsRequired();

        builder.Property(lp => lp.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(lp => lp.ProviderSlug)
            .HasColumnName("provider_slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lp => lp.ProviderName)
            .HasColumnName("provider_name")
            .HasMaxLength(200);

        builder.Property(lp => lp.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(lp => lp.ProviderLeagueId)
            .HasColumnName("provider_league_id");

        builder.Property(lp => lp.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(lp => lp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(lp => lp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign Keys
        builder.HasOne(lp => lp.League)
            .WithMany(l => l.LeagueProviders)
            .HasForeignKey(lp => lp.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lp => lp.Provider)
            .WithMany(p => p.LeagueProviders)
            .HasForeignKey(lp => lp.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(lp => new { lp.LeagueId, lp.ProviderId })
            .IsUnique()
            .HasDatabaseName("ix_league_providers_league_provider");

        // Unique constraint - provider + slug combination must be unique
        builder.HasIndex(lp => new { lp.ProviderId, lp.ProviderSlug })
            .IsUnique()
            .HasDatabaseName("ix_league_providers_provider_id_provider_slug");

        builder.HasIndex(lp => lp.ProviderSlug)
            .HasDatabaseName("ix_league_providers_provider_slug");

        builder.HasIndex(lp => lp.IsActive)
            .HasDatabaseName("ix_league_providers_is_active");
    }
}
