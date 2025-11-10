using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededCountriesAndLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove seeded data (countries and leagues should be created through scan/import workflow)
            // Order matters due to FK constraints: LeagueProviders -> Leagues -> Countries

            // 1. Delete LeagueProvider mappings for seeded leagues
            migrationBuilder.Sql(@"
                DELETE FROM configuration.league_providers
                WHERE league_id IN (
                    SELECT id FROM configuration.leagues
                    WHERE name IN ('Premier League', 'La Liga', 'Bundesliga', 'Serie A', 'Ligue 1')
                );
            ");

            // 2. Delete seeded leagues
            migrationBuilder.Sql(@"
                DELETE FROM configuration.leagues
                WHERE name IN ('Premier League', 'La Liga', 'Bundesliga', 'Serie A', 'Ligue 1');
            ");

            // 3. Delete CountryProvider mappings for seeded countries
            migrationBuilder.Sql(@"
                DELETE FROM configuration.country_providers
                WHERE country_id IN (
                    SELECT id FROM configuration.countries
                    WHERE code IN ('england', 'spain', 'germany', 'italy', 'france')
                );
            ");

            // 4. Delete seeded countries
            migrationBuilder.Sql(@"
                DELETE FROM configuration.countries
                WHERE code IN ('england', 'spain', 'germany', 'italy', 'france');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
