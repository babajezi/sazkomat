using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiresHintToScraperRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "requires_hint",
                schema: "data_import",
                table: "scraper_recipes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requires_hint",
                schema: "data_import",
                table: "scraper_recipes");
        }
    }
}
