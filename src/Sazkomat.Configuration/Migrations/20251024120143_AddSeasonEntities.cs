using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seasons",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: false),
                    end_year = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "league_seasons",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_available_on_betexplorer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    has_data = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_odds = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rounds_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    matches_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_seasons", x => x.id);
                    table.ForeignKey(
                        name: "FK_league_seasons_leagues_league_id",
                        column: x => x.league_id,
                        principalSchema: "configuration",
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_league_seasons_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "configuration",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_has_data",
                schema: "configuration",
                table: "league_seasons",
                column: "has_data");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_last_scraped_at",
                schema: "configuration",
                table: "league_seasons",
                column: "last_scraped_at");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_league_id",
                schema: "configuration",
                table: "league_seasons",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_league_id_season_id",
                schema: "configuration",
                table: "league_seasons",
                columns: new[] { "league_id", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_season_id",
                schema: "configuration",
                table: "league_seasons",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_seasons_name",
                schema: "configuration",
                table: "seasons",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_start_year_end_year",
                schema: "configuration",
                table: "seasons",
                columns: new[] { "start_year", "end_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_seasons",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "seasons",
                schema: "configuration");
        }
    }
}
