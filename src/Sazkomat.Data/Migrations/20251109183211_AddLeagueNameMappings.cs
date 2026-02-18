using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueNameMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "league_name_mappings",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    provider_league_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    betexplorer_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_name_mappings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_league_name_mappings_country_code",
                schema: "data_import",
                table: "league_name_mappings",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_league_name_mappings_lookup",
                schema: "data_import",
                table: "league_name_mappings",
                columns: new[] { "provider_code", "country_code", "provider_league_name", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_league_name_mappings_provider_code",
                schema: "data_import",
                table: "league_name_mappings",
                column: "provider_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_name_mappings",
                schema: "data_import");
        }
    }
}
