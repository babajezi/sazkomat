using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnmatchedCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "unmatched_countries",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_country_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider_country_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolution_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    resolved_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unmatched_countries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_countries_is_resolved",
                schema: "data_import",
                table: "unmatched_countries",
                column: "is_resolved");

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_countries_provider_id",
                schema: "data_import",
                table: "unmatched_countries",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_unmatched_countries_unique",
                schema: "data_import",
                table: "unmatched_countries",
                columns: new[] { "provider_id", "provider_country_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unmatched_countries",
                schema: "data_import");
        }
    }
}
