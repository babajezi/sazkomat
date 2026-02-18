using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches", "data_import");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.RoundId).HasColumnName("round_id").IsRequired();
        builder.Property(m => m.ProviderId).HasColumnName("provider_id").IsRequired();
        builder.Property(m => m.HomeTeam).HasColumnName("home_team").HasMaxLength(200).IsRequired();
        builder.Property(m => m.AwayTeam).HasColumnName("away_team").HasMaxLength(200).IsRequired();
        builder.Property(m => m.HomeScore).HasColumnName("home_score").IsRequired();
        builder.Property(m => m.AwayScore).HasColumnName("away_score").IsRequired();
        builder.Property(m => m.Result).HasColumnName("result").HasMaxLength(1).IsRequired();
        builder.Property(m => m.HomeOdds).HasColumnName("home_odds").HasColumnType("decimal(10,2)");
        builder.Property(m => m.DrawOdds).HasColumnName("draw_odds").HasColumnType("decimal(10,2)");
        builder.Property(m => m.AwayOdds).HasColumnName("away_odds").HasColumnType("decimal(10,2)");
        builder.Property(m => m.MatchDate).HasColumnName("match_date");
        builder.Property(m => m.BetExplorerUrl).HasColumnName("betexplorer_url").HasMaxLength(500);
        builder.Property(m => m.ProviderUrl).HasColumnName("provider_url").HasMaxLength(500);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Relationship with Round
        builder.HasOne(m => m.Round)
            .WithMany(r => r.Matches)
            .HasForeignKey(m => m.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => m.RoundId);
        builder.HasIndex(m => m.ProviderId);
        builder.HasIndex(m => m.MatchDate);
        builder.HasIndex(m => m.Result);
        builder.HasIndex(m => new { m.HomeTeam, m.AwayTeam });
    }
}
