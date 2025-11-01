using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class UseLeagueProviderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leagues_sport_id_country_id_name",
                schema: "configuration",
                table: "leagues");

            migrationBuilder.DropIndex(
                name: "IX_league_providers_provider_id",
                schema: "configuration",
                table: "league_providers");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "configuration",
                table: "countries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_name",
                schema: "configuration",
                table: "leagues",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_league_providers_provider_id_provider_slug",
                schema: "configuration",
                table: "league_providers",
                columns: new[] { "provider_id", "provider_slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leagues_name",
                schema: "configuration",
                table: "leagues");

            migrationBuilder.DropIndex(
                name: "ix_league_providers_provider_id_provider_slug",
                schema: "configuration",
                table: "league_providers");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "configuration",
                table: "countries");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_sport_id_country_id_name",
                schema: "configuration",
                table: "leagues",
                columns: new[] { "sport_id", "country_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_providers_provider_id",
                schema: "configuration",
                table: "league_providers",
                column: "provider_id");
        }
    }
}
