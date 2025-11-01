using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data.Configurations;

public class SyncWorkflowStateConfiguration : IEntityTypeConfiguration<SyncWorkflowState>
{
    public void Configure(EntityTypeBuilder<SyncWorkflowState> builder)
    {
        builder.ToTable("sync_workflow_state", "configuration");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.CountriesSynced)
            .HasColumnName("countries_synced")
            .IsRequired();

        builder.Property(s => s.CountriesConfirmed)
            .HasColumnName("countries_confirmed")
            .IsRequired();

        builder.Property(s => s.LeaguesSynced)
            .HasColumnName("leagues_synced")
            .IsRequired();

        builder.Property(s => s.LeaguesConfirmed)
            .HasColumnName("leagues_confirmed")
            .IsRequired();

        builder.Property(s => s.SeasonsSynced)
            .HasColumnName("seasons_synced")
            .IsRequired();

        builder.Property(s => s.CountriesSyncedAt)
            .HasColumnName("countries_synced_at");

        builder.Property(s => s.LeaguesSyncedAt)
            .HasColumnName("leagues_synced_at");

        builder.Property(s => s.SeasonsSyncedAt)
            .HasColumnName("seasons_synced_at");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
