using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncFlagsToLeagueSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_current",
                schema: "configuration",
                table: "league_seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_data_sync_at",
                schema: "configuration",
                table: "league_seasons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sync_enabled",
                schema: "configuration",
                table: "league_seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sync_mode",
                schema: "configuration",
                table: "league_seasons",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Historical");

            migrationBuilder.AddColumn<string>(
                name: "current_season_patterns",
                schema: "configuration",
                table: "data_providers",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_is_current",
                schema: "configuration",
                table: "league_seasons",
                column: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_league_seasons_sync_enabled",
                schema: "configuration",
                table: "league_seasons",
                column: "sync_enabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_league_seasons_is_current",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropIndex(
                name: "IX_league_seasons_sync_enabled",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "is_current",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "last_data_sync_at",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "sync_enabled",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "sync_mode",
                schema: "configuration",
                table: "league_seasons");

            migrationBuilder.DropColumn(
                name: "current_season_patterns",
                schema: "configuration",
                table: "data_providers");
        }
    }
}
