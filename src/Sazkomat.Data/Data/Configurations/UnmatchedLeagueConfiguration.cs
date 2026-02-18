using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class UnmatchedLeagueConfiguration : IEntityTypeConfiguration<UnmatchedLeague>
{
    public void Configure(EntityTypeBuilder<UnmatchedLeague> builder)
    {
        builder.ToTable("unmatched_leagues", "data_import");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.ProviderId)
            .HasColumnName("provider_id")
            .IsRequired();

        builder.Property(u => u.ProviderLeagueId)
            .HasColumnName("provider_league_id")
            .HasMaxLength(100);

        builder.Property(u => u.ProviderLeagueName)
            .HasColumnName("provider_league_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.ProviderSlug)
            .HasColumnName("provider_slug")
            .HasMaxLength(200);

        builder.Property(u => u.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.CountryName)
            .HasColumnName("country_name")
            .HasMaxLength(100);

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

        builder.Property(u => u.ResolvedLeagueId)
            .HasColumnName("resolved_league_id");

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

        // Relationships
        builder.HasOne(u => u.Provider)
            .WithMany()
            .HasForeignKey(u => u.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.ResolvedLeague)
            .WithMany()
            .HasForeignKey(u => u.ResolvedLeagueId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(u => u.ProviderId)
            .HasDatabaseName("ix_unmatched_leagues_provider_id");

        builder.HasIndex(u => u.IsResolved)
            .HasDatabaseName("ix_unmatched_leagues_is_resolved");

        builder.HasIndex(u => new { u.ProviderId, u.CountryCode })
            .HasDatabaseName("ix_unmatched_leagues_provider_country");

        // Unique constraint - same league from same provider should not be duplicated
        builder.HasIndex(u => new { u.ProviderId, u.ProviderLeagueName, u.CountryCode })
            .IsUnique()
            .HasDatabaseName("ix_unmatched_leagues_unique");
    }
}
