using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries", "configuration");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.NameCs)
            .HasColumnName("name_cs")
            .HasMaxLength(100);

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.FlagEmoji)
            .HasColumnName("flag_emoji")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Unique constraints
        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasIndex(c => c.Code)
            .IsUnique();
    }
}
