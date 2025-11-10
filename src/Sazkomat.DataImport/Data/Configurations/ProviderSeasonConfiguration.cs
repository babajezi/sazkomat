using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data.Configurations;

public class ProviderSeasonConfiguration : IEntityTypeConfiguration<ProviderSeason>
{
    public void Configure(EntityTypeBuilder<ProviderSeason> builder)
    {
        builder.ToTable("provider_seasons", "data_import");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id).HasColumnName("id");
        builder.Property(ps => ps.ProviderId).HasColumnName("provider_id").IsRequired();
        builder.Property(ps => ps.ProviderLeagueId).HasColumnName("provider_league_id").IsRequired();
        builder.Property(ps => ps.SeasonName).HasColumnName("season_name").IsRequired().HasMaxLength(100);
        builder.Property(ps => ps.StartYear).HasColumnName("start_year").IsRequired();
        builder.Property(ps => ps.EndYear).HasColumnName("end_year");
        builder.Property(ps => ps.IsCurrentSeason).HasColumnName("is_current_season").IsRequired();
        builder.Property(ps => ps.ScrapedAt).HasColumnName("scraped_at").IsRequired();
        builder.Property(ps => ps.RawData).HasColumnName("raw_data").HasColumnType("jsonb");

        builder.Property(ps => ps.IsImported).HasColumnName("is_imported").IsRequired();
        builder.Property(ps => ps.SeasonId).HasColumnName("season_id");
        builder.Property(ps => ps.ImportedAt).HasColumnName("imported_at");

        builder.Property(ps => ps.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(ps => ps.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore navigation properties
        builder.Ignore(ps => ps.Provider);
        builder.Ignore(ps => ps.ProviderLeague);
        builder.Ignore(ps => ps.Season);

        // Indexes
        builder.HasIndex(ps => ps.ProviderId);
        builder.HasIndex(ps => ps.ProviderLeagueId);
        builder.HasIndex(ps => ps.IsCurrentSeason);
        builder.HasIndex(ps => ps.IsImported);
        builder.HasIndex(ps => ps.SeasonId);
        builder.HasIndex(ps => ps.ScrapedAt);
    }
}
