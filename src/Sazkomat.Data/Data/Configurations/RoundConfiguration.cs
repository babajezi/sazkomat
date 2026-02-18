using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("rounds", "data_import");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.LeagueId).HasColumnName("league_id").IsRequired();
        builder.Property(r => r.SeasonId).HasColumnName("season_id").IsRequired();
        builder.Property(r => r.ProviderId).HasColumnName("provider_id").IsRequired();
        builder.Property(r => r.RoundNumber).HasColumnName("round_number").IsRequired();
        builder.Property(r => r.GroupName).HasColumnName("group_name").HasMaxLength(50);
        builder.Property(r => r.StartDate).HasColumnName("start_date");
        builder.Property(r => r.EndDate).HasColumnName("end_date");
        builder.Property(r => r.MatchesCount).HasColumnName("matches_count").IsRequired();
        builder.Property(r => r.HomeWins).HasColumnName("home_wins").IsRequired();
        builder.Property(r => r.Draws).HasColumnName("draws").IsRequired();
        builder.Property(r => r.AwayWins).HasColumnName("away_wins").IsRequired();
        builder.Property(r => r.CumulativeOddsHome).HasColumnName("cumulative_odds_home").HasColumnType("decimal(18,4)");
        builder.Property(r => r.CumulativeOddsDraw).HasColumnName("cumulative_odds_draw").HasColumnType("decimal(18,4)");
        builder.Property(r => r.CumulativeOddsAway).HasColumnName("cumulative_odds_away").HasColumnType("decimal(18,4)");
        builder.Property(r => r.SummaryResult).HasColumnName("summary_result").HasMaxLength(50).IsRequired();
        builder.Property(r => r.OddsComplete).HasColumnName("odds_complete").HasMaxLength(10).IsRequired();
        builder.Property(r => r.ScrapedAt).HasColumnName("scraped_at").IsRequired();
        builder.Property(r => r.DataSource).HasColumnName("data_source").HasMaxLength(100).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // League navigation is managed by ConfigurationDbContext
        builder.Ignore(r => r.League);

        // Season FK - enables cross-schema JOIN for efficient sorting
        builder.HasOne(r => r.Season)
            .WithMany()
            .HasForeignKey(r => r.SeasonId)
            .IsRequired();

        // Unique constraint (includes GroupName for leagues with groups)
        builder.HasIndex(r => new { r.LeagueId, r.SeasonId, r.GroupName, r.RoundNumber })
            .IsUnique();

        // Indexes
        builder.HasIndex(r => new { r.LeagueId, r.SeasonId, r.GroupName });
        builder.HasIndex(r => r.ScrapedAt);
        builder.HasIndex(r => r.SeasonId);
        builder.HasIndex(r => r.ProviderId);
    }
}
