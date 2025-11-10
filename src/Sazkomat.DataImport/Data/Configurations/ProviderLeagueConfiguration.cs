using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data.Configurations;

public class ProviderLeagueConfiguration : IEntityTypeConfiguration<ProviderLeague>
{
    public void Configure(EntityTypeBuilder<ProviderLeague> builder)
    {
        builder.ToTable("provider_leagues", "data_import");

        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.Id).HasColumnName("id");
        builder.Property(pl => pl.ProviderId).HasColumnName("provider_id").IsRequired();
        builder.Property(pl => pl.ProviderCountryId).HasColumnName("provider_country_id").IsRequired();
        builder.Property(pl => pl.ProviderSlug).HasColumnName("provider_slug").IsRequired().HasMaxLength(500);
        builder.Property(pl => pl.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(500);
        builder.Property(pl => pl.DisplayName).HasColumnName("display_name").HasMaxLength(500);
        builder.Property(pl => pl.Priority).HasColumnName("priority").IsRequired();
        builder.Property(pl => pl.IsBettable).HasColumnName("is_bettable").IsRequired();
        builder.Property(pl => pl.ScrapedAt).HasColumnName("scraped_at").IsRequired();
        builder.Property(pl => pl.RawData).HasColumnName("raw_data").HasColumnType("jsonb");

        builder.Property(pl => pl.IsImported).HasColumnName("is_imported").IsRequired();
        builder.Property(pl => pl.LeagueId).HasColumnName("league_id");
        builder.Property(pl => pl.ImportedAt).HasColumnName("imported_at");

        builder.Property(pl => pl.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(pl => pl.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore navigation properties
        builder.Ignore(pl => pl.Provider);
        builder.Ignore(pl => pl.ProviderCountry);
        builder.Ignore(pl => pl.League);
        builder.Ignore(pl => pl.ProviderSeasons);

        // Indexes
        builder.HasIndex(pl => pl.ProviderId);
        builder.HasIndex(pl => pl.ProviderCountryId);
        builder.HasIndex(pl => pl.ProviderSlug);
        builder.HasIndex(pl => pl.IsImported);
        builder.HasIndex(pl => pl.LeagueId);
        builder.HasIndex(pl => pl.ScrapedAt);
    }
}
