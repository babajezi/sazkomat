using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Data.Configurations;

public class ScraperRecipeConfiguration : IEntityTypeConfiguration<ScraperRecipe>
{
    public void Configure(EntityTypeBuilder<ScraperRecipe> builder)
    {
        builder.ToTable("scraper_recipes", "data_import");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(r => r.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.PageType)
            .HasColumnName("page_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.ActionsJson)
            .HasColumnName("actions_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(r => r.RoundHeaderSelector)
            .HasColumnName("round_header_selector")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.GroupPatternRegex)
            .HasColumnName("group_pattern_regex")
            .HasMaxLength(200);

        builder.Property(r => r.MatchRowSelector)
            .HasColumnName("match_row_selector")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.OddsCellSelector)
            .HasColumnName("odds_cell_selector")
            .HasMaxLength(500);

        builder.Property(r => r.RequiresHint)
            .HasColumnName("requires_hint")
            .HasMaxLength(100);

        builder.Property(r => r.TotalAttempts)
            .HasColumnName("total_attempts")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.SuccessfulAttempts)
            .HasColumnName("successful_attempts")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(r => new { r.Provider, r.PageType })
            .HasDatabaseName("ix_scraper_recipes_provider_page_type");

        builder.HasIndex(r => r.Priority)
            .HasDatabaseName("ix_scraper_recipes_priority");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("ix_scraper_recipes_is_active");

        // Unique name per provider/page type combination
        builder.HasIndex(r => new { r.Provider, r.PageType, r.Name })
            .IsUnique()
            .HasDatabaseName("ix_scraper_recipes_unique_name");
    }
}
