using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matches",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    away_team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    home_score = table.Column<int>(type: "integer", nullable: false),
                    away_score = table.Column<int>(type: "integer", nullable: false),
                    result = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    home_odds = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    draw_odds = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    away_odds = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    match_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    betexplorer_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_matches_rounds_round_id",
                        column: x => x.round_id,
                        principalSchema: "data_import",
                        principalTable: "rounds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matches_home_team_away_team",
                schema: "data_import",
                table: "matches",
                columns: new[] { "home_team", "away_team" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_match_date",
                schema: "data_import",
                table: "matches",
                column: "match_date");

            migrationBuilder.CreateIndex(
                name: "IX_matches_result",
                schema: "data_import",
                table: "matches",
                column: "result");

            migrationBuilder.CreateIndex(
                name: "IX_matches_round_id",
                schema: "data_import",
                table: "matches",
                column: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matches",
                schema: "data_import");
        }
    }
}
