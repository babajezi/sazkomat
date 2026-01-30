using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeTrackingToLeagueSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "last_successful_recipe_id",
                schema: "configuration",
                table: "league_seasons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_recipe_tested_at",
                schema: "configuration",
                table: "league_seasons",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_successful_recipe_id",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "last_recipe_tested_at",
                schema: "configuration",
                table: "league_seasons");
        }
    }
}
