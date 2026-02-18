using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnmatchedLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "unmatched_leagues",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_league_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider_league_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    provider_slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    country_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolution_type = table.Column<int>(type: "integer", nullable: true),
                    resolved_league_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unmatched_leagues", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_leagues_country_code",
                schema: "data_import",
                table: "unmatched_leagues",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_leagues_is_resolved",
                schema: "data_import",
                table: "unmatched_leagues",
                column: "is_resolved");

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_leagues_provider_id",
                schema: "data_import",
                table: "unmatched_leagues",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_leagues_unique",
                schema: "data_import",
                table: "unmatched_leagues",
                columns: new[] { "provider_id", "provider_league_name", "country_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unmatched_leagues",
                schema: "data_import");
        }
    }
}
