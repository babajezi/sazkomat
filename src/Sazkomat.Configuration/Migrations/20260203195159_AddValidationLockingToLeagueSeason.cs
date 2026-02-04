using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationLockingToLeagueSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_locked",
                schema: "configuration",
                table: "league_seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_validated_at",
                schema: "configuration",
                table: "league_seasons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_at",
                schema: "configuration",
                table: "league_seasons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_is_locked",
                schema: "configuration",
                table: "league_seasons",
                column: "is_locked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_league_seasons_is_locked",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "is_locked",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "last_validated_at",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "locked_at",
                schema: "configuration",
                table: "league_seasons");
        }
    }
}
