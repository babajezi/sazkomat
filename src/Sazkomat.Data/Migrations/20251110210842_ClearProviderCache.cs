using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClearProviderCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear all provider cache data (all countries, leagues, and seasons have IsImported=true with outdated CountryIds)
            // Order matters due to FK constraints: ProviderSeasons -> ProviderLeagues -> ProviderCountries

            // 1. Delete all provider seasons
            migrationBuilder.Sql(@"
                DELETE FROM data_import.provider_seasons;
            ");

            // 2. Delete all provider leagues
            migrationBuilder.Sql(@"
                DELETE FROM data_import.provider_leagues;
            ");

            // 3. Delete all provider countries
            migrationBuilder.Sql(@"
                DELETE FROM data_import.provider_countries;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
