using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("leagues", "configuration");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.SportId)
            .HasColumnName("sport_id")
            .IsRequired();

        builder.Property(l => l.CountryId)
            .HasColumnName("country_id")
            .IsRequired();

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.NameCs)
            .HasColumnName("name_cs")
            .HasMaxLength(200);

        builder.Property(l => l.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(l => l.BetExplorerSlug)
            .HasColumnName("bet_explorer_slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.IsBettable)
            .HasColumnName("is_bettable")
            .IsRequired();

        builder.Property(l => l.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(l => l.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign keys
        builder.HasOne(l => l.Sport)
            .WithMany(s => s.Leagues)
            .HasForeignKey(l => l.SportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Country)
            .WithMany(c => c.Leagues)
            .HasForeignKey(l => l.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes (removed unique constraint on Name - using LeagueProvider mapping instead)
        builder.HasIndex(l => l.SportId);
        builder.HasIndex(l => l.CountryId);
        builder.HasIndex(l => l.Name); // Non-unique index for faster queries
        builder.HasIndex(l => l.IsBettable);
        builder.HasIndex(l => l.IsActive);
    }
}
