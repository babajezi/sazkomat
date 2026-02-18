using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add foreign key from data_import.rounds.league_id to configuration.leagues.id
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.rounds
                ADD CONSTRAINT FK_rounds_leagues_league_id
                FOREIGN KEY (league_id)
                REFERENCES configuration.leagues(id)
                ON DELETE CASCADE;
            ");

            // Add foreign key from data_import.import_jobs.league_id to configuration.leagues.id
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.import_jobs
                ADD CONSTRAINT FK_import_jobs_leagues_league_id
                FOREIGN KEY (league_id)
                REFERENCES configuration.leagues(id)
                ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key from data_import.import_jobs
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.import_jobs
                DROP CONSTRAINT IF EXISTS FK_import_jobs_leagues_league_id;
            ");

            // Drop foreign key from data_import.rounds
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.rounds
                DROP CONSTRAINT IF EXISTS FK_rounds_leagues_league_id;
            ");
        }
    }
}
