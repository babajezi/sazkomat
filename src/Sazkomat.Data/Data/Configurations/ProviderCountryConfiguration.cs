using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Data.Configurations;

public class ProviderCountryConfiguration : IEntityTypeConfiguration<ProviderCountry>
{
    public void Configure(EntityTypeBuilder<ProviderCountry> builder)
    {
        builder.ToTable("provider_countries", "data_import");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Id).HasColumnName("id");
        builder.Property(pc => pc.ProviderId).HasColumnName("provider_id").IsRequired();
        builder.Property(pc => pc.ProviderCode).HasColumnName("provider_code").IsRequired().HasMaxLength(200);
        builder.Property(pc => pc.ProviderName).HasColumnName("provider_name").IsRequired().HasMaxLength(500);
        builder.Property(pc => pc.IsoCode).HasColumnName("iso_code").HasMaxLength(10);
        builder.Property(pc => pc.FlagEmoji).HasColumnName("flag_emoji").HasMaxLength(10);
        builder.Property(pc => pc.ScrapedAt).HasColumnName("scraped_at").IsRequired();
        builder.Property(pc => pc.RawData).HasColumnName("raw_data").HasColumnType("jsonb");

        builder.Property(pc => pc.IsImported).HasColumnName("is_imported").IsRequired();
        builder.Property(pc => pc.CountryId).HasColumnName("country_id");
        builder.Property(pc => pc.ImportedAt).HasColumnName("imported_at");

        builder.Property(pc => pc.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(pc => pc.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore navigation properties (managed in ConfigurationDbContext)
        builder.Ignore(pc => pc.Provider);
        builder.Ignore(pc => pc.Country);
        builder.Ignore(pc => pc.ProviderLeagues);

        // Indexes
        builder.HasIndex(pc => pc.ProviderId);
        builder.HasIndex(pc => pc.ProviderCode);
        builder.HasIndex(pc => pc.IsImported);
        builder.HasIndex(pc => pc.CountryId);
        builder.HasIndex(pc => pc.ScrapedAt);
    }
}
