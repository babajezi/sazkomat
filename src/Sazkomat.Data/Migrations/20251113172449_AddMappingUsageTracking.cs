using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMappingUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "last_provider_league_id",
                schema: "data_import",
                table: "league_name_mappings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                schema: "data_import",
                table: "league_name_mappings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "usage_count",
                schema: "data_import",
                table: "league_name_mappings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_provider_league_id",
                schema: "data_import",
                table: "league_name_mappings");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                schema: "data_import",
                table: "league_name_mappings");

            migrationBuilder.DropColumn(
                name: "usage_count",
                schema: "data_import",
                table: "league_name_mappings");
        }
    }
}
