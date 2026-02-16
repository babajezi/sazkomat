using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class LeagueSeasonConfiguration : IEntityTypeConfiguration<LeagueSeason>
{
    public void Configure(EntityTypeBuilder<LeagueSeason> builder)
    {
        builder.ToTable("league_seasons", "configuration");

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.Id)
            .HasColumnName("id");

        builder.Property(ls => ls.LeagueId)
            .HasColumnName("league_id")
            .IsRequired();

        builder.Property(ls => ls.SeasonId)
            .HasColumnName("season_id")
            .IsRequired();

        builder.Property(ls => ls.IsAvailableOnBetExplorer)
            .HasColumnName("is_available_on_betexplorer")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ls => ls.HasData)
            .HasColumnName("has_data")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.NoDataReason)
            .HasColumnName("no_data_reason")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(ls => ls.NoDataNote)
            .HasColumnName("no_data_note")
            .HasMaxLength(500);

        builder.Property(ls => ls.HasOdds)
            .HasColumnName("has_odds")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.LastScrapedAt)
            .HasColumnName("last_scraped_at");

        builder.Property(ls => ls.RoundsCount)
            .HasColumnName("rounds_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ls => ls.MatchesCount)
            .HasColumnName("matches_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(ls => ls.SyncEnabled)
            .HasColumnName("sync_enabled")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.IsCurrent)
            .HasColumnName("is_current")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.SyncMode)
            .HasColumnName("sync_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(SyncMode.Historical);

        builder.Property(ls => ls.LastDataSyncAt)
            .HasColumnName("last_data_sync_at");

        builder.Property(ls => ls.LastSuccessfulRecipeId)
            .HasColumnName("last_successful_recipe_id");

        builder.Property(ls => ls.LastRecipeTestedAt)
            .HasColumnName("last_recipe_tested_at");

        builder.Property(ls => ls.IsLocked)
            .HasColumnName("is_locked")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.LockedAt)
            .HasColumnName("locked_at");

        builder.Property(ls => ls.LastValidatedAt)
            .HasColumnName("last_validated_at");

        builder.Property(ls => ls.IsIgnored)
            .HasColumnName("is_ignored")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ls => ls.IgnoredAt)
            .HasColumnName("ignored_at");

        builder.Property(ls => ls.IgnoredNote)
            .HasColumnName("ignored_note")
            .HasMaxLength(500);

        builder.Property(ls => ls.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ls => ls.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign keys
        builder.HasOne(ls => ls.League)
            .WithMany(l => l.LeagueSeasons)
            .HasForeignKey(ls => ls.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ls => ls.Season)
            .WithMany(s => s.LeagueSeasons)
            .HasForeignKey(ls => ls.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint - each league can only have one record per season
        builder.HasIndex(ls => new { ls.LeagueId, ls.SeasonId })
            .IsUnique();

        // Indexes for queries
        builder.HasIndex(ls => ls.LeagueId);
        builder.HasIndex(ls => ls.SeasonId);
        builder.HasIndex(ls => ls.HasData);
        builder.HasIndex(ls => ls.LastScrapedAt);
        builder.HasIndex(ls => ls.SyncEnabled);
        builder.HasIndex(ls => ls.IsCurrent);
        builder.HasIndex(ls => ls.IsLocked);
        builder.HasIndex(ls => ls.IsIgnored);
    }
}
