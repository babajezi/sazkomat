using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scraper_recipes",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    page_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    actions_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    round_header_selector = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    group_pattern_regex = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    match_row_selector = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    odds_cell_selector = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    successful_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scraper_recipes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scraper_recipes_is_active",
                schema: "data_import",
                table: "scraper_recipes",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_scraper_recipes_priority",
                schema: "data_import",
                table: "scraper_recipes",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_scraper_recipes_provider_page_type",
                schema: "data_import",
                table: "scraper_recipes",
                columns: new[] { "provider", "page_type" });

            migrationBuilder.CreateIndex(
                name: "ix_scraper_recipes_unique_name",
                schema: "data_import",
                table: "scraper_recipes",
                columns: new[] { "provider", "page_type", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scraper_recipes",
                schema: "data_import");
        }
    }
}
