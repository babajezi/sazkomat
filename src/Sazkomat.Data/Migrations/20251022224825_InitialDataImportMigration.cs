using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataImportMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data_import");

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    seasons = table.Column<string>(type: "jsonb", nullable: false),
                    include_without_odds = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    progress = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    matches_count = table.Column<int>(type: "integer", nullable: false),
                    home_wins = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    away_wins = table.Column<int>(type: "integer", nullable: false),
                    cumulative_odds_home = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_odds_draw = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cumulative_odds_away = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    summary_result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    odds_complete = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rounds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_league_id",
                schema: "data_import",
                table: "import_jobs",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_status",
                schema: "data_import",
                table: "import_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season" });

            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season_round_number",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rounds_scraped_at",
                schema: "data_import",
                table: "rounds",
                column: "scraped_at");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_season",
                schema: "data_import",
                table: "rounds",
                column: "season");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "data_import");

            migrationBuilder.DropTable(
                name: "rounds",
                schema: "data_import");
        }
    }
}
