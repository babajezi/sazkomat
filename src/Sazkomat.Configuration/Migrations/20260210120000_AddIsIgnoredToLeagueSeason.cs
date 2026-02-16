using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddIsIgnoredToLeagueSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_ignored",
                schema: "configuration",
                table: "league_seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ignored_at",
                schema: "configuration",
                table: "league_seasons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ignored_note",
                schema: "configuration",
                table: "league_seasons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_is_ignored",
                schema: "configuration",
                table: "league_seasons",
                column: "is_ignored");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_league_seasons_is_ignored",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "is_ignored",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "ignored_at",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "ignored_note",
                schema: "configuration",
                table: "league_seasons");
        }
    }
}
